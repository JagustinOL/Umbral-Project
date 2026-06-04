import { getPlayerTeamMembership } from './teamService';

export async function assertPlayerCanJoinOrCreateTeam(
  playerId: string,
): Promise<void> {
  const membership = await getPlayerTeamMembership(playerId);

  if (membership.isMember && membership.teamId) {
    throw new Error(
      'You already belong to an active team. Leave or disband it before joining or creating another.',
    );
  }

  if (membership.hasPendingJoinRequest) {
    throw new Error(
      'You already have a pending join request. Wait for the leader to approve or reject it.',
    );
  }
}

export async function resolveTeamMembership(playerId: string): Promise<{
  teamId: string | null;
  pendingTeamId: string | null;
}> {
  const membership = await getPlayerTeamMembership(playerId);

  if (membership.isMember && membership.teamId) {
    return {
      teamId: membership.teamId.toLowerCase(),
      pendingTeamId: null,
    };
  }

  if (membership.hasPendingJoinRequest && membership.pendingTeamId) {
    return {
      teamId: null,
      pendingTeamId: membership.pendingTeamId.toLowerCase(),
    };
  }

  return {
    teamId: null,
    pendingTeamId: null,
  };
}
