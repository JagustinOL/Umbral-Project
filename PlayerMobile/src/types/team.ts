export type TeamRole = 'Leader' | 'Member';

export type TeamMember = {
  playerRef: string;
  displayName: string;
  role: TeamRole;
  joinedAtUtc: string;
};

export type JoinRequest = {
  requestId: string;
  playerRef: string;
  displayName: string;
  status: 'Pending' | 'Approved' | 'Rejected';
  requestedAtUtc: string;
};

export type TeamDetails = {
  teamId: string;
  name: string;
  teamCode: string;
  isLocked: boolean;
  currentSessionRef: string | null;
  /** Sesión con solicitud de unión pendiente del equipo (compartida entre miembros). */
  pendingSessionJoinRef: string | null;
  isDisbanded: boolean;
  members: TeamMember[];
  pendingRequests: JoinRequest[];
};
