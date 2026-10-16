import type { RequestHandler } from 'express';
import type { Types } from 'mongoose';
import {
  canFamily,
  canPropose,
  type FamilyPermission,
  type FamilyPermissionTarget,
  type FamilyViewerRole,
} from '@fh/contracts';
import { AppError } from '../../core/errors.js';
import { parseIdParam } from '../../core/validation.js';
import { FamilyModel, type FamilyDoc } from './family.model.js';
import { FamilyMembershipModel, type FamilyMembershipDoc } from './membership.model.js';

/** The caller's standing in one family, resolved once per request. */
export class FamilyAccess {
  constructor(
    readonly family: FamilyDoc,
    readonly membership: FamilyMembershipDoc | null,
  ) {}

  get role(): FamilyViewerRole {
    return this.membership?.role ?? 'guest';
  }

  get familyId(): Types.ObjectId {
    return this.family._id;
  }

  get isPublicFamily(): boolean {
    return this.family.privacy?.visibility !== 'private';
  }

  private get context() {
    return {
      role: this.role,
      moderatedBranchIds: (this.membership?.moderatedBranchIds ?? []).map((id) => id.toString()),
    };
  }

  can(permission: FamilyPermission, target: FamilyPermissionTarget = {}): boolean {
    return canFamily(this.context, permission, target);
  }

  canPropose(permission: FamilyPermission): boolean {
    return canPropose(this.context, permission);
  }

  assert(permission: FamilyPermission, target: FamilyPermissionTarget = {}): void {
    if (!this.can(permission, target)) throw AppError.forbidden();
  }
}

export async function resolveFamilyAccess(familyId: Types.ObjectId, userId: Types.ObjectId | undefined): Promise<FamilyAccess | null> {
  const family = await FamilyModel.findById(familyId);
  if (!family) return null;
  const membership = userId ? await FamilyMembershipModel.findOne({ familyId, userId, status: 'active' }) : null;
  const access = new FamilyAccess(family, membership);
  // Non-members never learn that a private family exists, nor see families withdrawn by the platform.
  if (!membership && (family.status !== 'active' || family.privacy?.visibility === 'private')) return null;
  return access;
}

/** Resolves `:familyId` and attaches `req.familyAccess`. Unknown and hidden families are both 404. */
export const loadFamilyAccess: RequestHandler = async (req, _res, next) => {
  const familyId = parseIdParam(req.params['familyId'], 'Family');
  const access = await resolveFamilyAccess(familyId, req.auth?.userId);
  if (!access) return next(AppError.notFound('Family'));
  req.familyAccess = access;
  next();
};

/**
 * Route-level permission check. Guests are evaluated against public data. Checks that depend on the
 * target record (branch scope, ownership) are made in services with `access.assert(permission, target)`.
 */
export function requireFamilyPermission(permission: FamilyPermission): RequestHandler {
  return (req, _res, next) => {
    const access = req.familyAccess;
    if (!access) return next(new Error('loadFamilyAccess must run before requireFamilyPermission'));
    if (access.can(permission, { isPublic: access.isPublicFamily })) return next();
    next(req.auth ? AppError.forbidden() : AppError.unauthenticated());
  };
}

export function accessOf(req: Express.Request): FamilyAccess {
  if (!req.familyAccess) throw new Error('Family access not resolved for this route');
  return req.familyAccess;
}
