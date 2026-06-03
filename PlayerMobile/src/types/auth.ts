export type PlayerProfile = {
  playerId: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive?: boolean;
};

export type AuthSession = {
  accessToken: string;
  refreshToken: string | null;
  expiresAt: number | null;
  player: PlayerProfile;
  teamId: string | null;
  pendingTeamId: string | null;
};
