/** Registers every Mongoose model so indexes are created for all collections at start-up. */
import './users/user.model.js';
import './auth/session.model.js';
import './auth/password-reset.model.js';
import './families/family.model.js';
import './families/membership.model.js';
import './members/member.model.js';
import './relationships/relationship.model.js';
import './branches/branch.model.js';
import './invitations/invitation.model.js';
import './contributions/contribution.model.js';
import './media/media.model.js';
import './events/event.model.js';
import './generations/generation-snapshot.model.js';
import './audit/audit-log.model.js';
