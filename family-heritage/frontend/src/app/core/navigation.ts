import {
  LucideArchive,
  LucideChartColumn,
  LucideGitFork,
  LucideImages,
  LucideLayoutDashboard,
  LucideLayers,
  LucideMail,
  LucideNetwork,
  LucideSettings,
  LucideShieldCheck,
  LucideUsers,
  LucideWaypoints,
  type LucideIcon,
} from '@lucide/angular';

export interface NavItem {
  label: string;
  path: string;
  icon: LucideIcon;
  /** Development phase that delivers the feature. Items with a phase are shown but disabled. */
  phase?: number;
  description: string;
  /** Visible only to platform super administrators. */
  superAdminOnly?: boolean;
}

export const NAV_ITEMS: readonly NavItem[] = [
  { label: 'Dashboard', path: '/dashboard', icon: LucideLayoutDashboard, description: 'Your families at a glance.' },
  { label: 'Family Tree', path: '/tree', icon: LucideNetwork, phase: 4, description: 'Interactive, zoomable genealogy of the whole family, from the founding ancestor down.' },
  { label: 'Family Members', path: '/members', icon: LucideUsers, phase: 2, description: 'Profiles of every person in the family, with search by name, generation, hometown and more.' },
  { label: 'Generations', path: '/generations', icon: LucideLayers, phase: 3, description: 'Automatically calculated generations, statistics and conflicts awaiting review.' },
  { label: 'Family Branches', path: '/branches', icon: LucideGitFork, phase: 4, description: 'Descendant lines of each child of the founder, with branch statistics.' },
  { label: 'Relationship Finder', path: '/relationships', icon: LucideWaypoints, phase: 5, description: 'Pick two relatives and see exactly how they are related, with the path between them.' },
  { label: 'Family History', path: '/history', icon: LucideArchive, phase: 7, description: 'Stories, oral histories, traditions and a timeline of family events.' },
  { label: 'Photos & Documents', path: '/media', icon: LucideImages, phase: 7, description: 'Photographs, recordings and documents with provenance and privacy controls.' },
  { label: 'Invitations', path: '/invitations', icon: LucideMail, phase: 6, description: 'Invite relatives by e-mail and let them claim their profile after verification.' },
  { label: 'Administration', path: '/admin', icon: LucideShieldCheck, superAdminOnly: true, description: 'Platform accounts and statistics.' },
  { label: 'Settings', path: '/settings', icon: LucideSettings, description: 'Your profile, password, appearance and signed-in devices.' },
];

export const STATS_ICON = LucideChartColumn;
