import { useCallback, useEffect, useState } from 'react';
import type { JoinRequest, TeamDetails } from '../types/team';
import * as teamService from '../services/teamService';
import { assertPlayerCanJoinOrCreateTeam } from '../services/playerTeamResolver';
import { isTeamLeaderRole, samePlayerRef } from '../utils/uuid';
import { useAuth } from './useAuth';

export function useTeamWorkspace() {
  const { session, setTeamId } = useAuth();
  const [team, setTeam] = useState<TeamDetails | null>(null);
  const [pendingRequests, setPendingRequests] = useState<JoinRequest[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const playerId = session?.player.playerId;
  const teamId = session?.teamId ?? null;

  const loadTeam = useCallback(async () => {
    if (!teamId) {
      setTeam(null);
      setPendingRequests([]);
      return;
    }

    setIsLoading(true);
    setError(null);
    try {
      const details = await teamService.getTeamById(teamId);
      setTeam(details);

      const currentMember = details.members.find((member) =>
        samePlayerRef(member.playerRef, playerId!),
      );
      if (isTeamLeaderRole(currentMember?.role)) {
        const requests = await teamService.getPendingRequests(teamId, playerId!);
        setPendingRequests(
          requests.filter((request) => request.status === 'Pending'),
        );
      } else {
        setPendingRequests([]);
      }
    } catch (loadError) {
      setError(
        loadError instanceof Error ? loadError.message : 'Failed to load team.',
      );
    } finally {
      setIsLoading(false);
    }
  }, [teamId, playerId]);

  useEffect(() => {
    loadTeam();
  }, [loadTeam]);

  const createTeam = useCallback(
    async (name: string) => {
      if (!session) {
        return;
      }

      await assertPlayerCanJoinOrCreateTeam(session.player.playerId);

      const displayName = `${session.player.firstName} ${session.player.lastName}`;
      const newTeamId = await teamService.createTeam({
        name,
        creatorId: session.player.playerId,
        creatorDisplayName: displayName,
      });
      await setTeamId(newTeamId, null);
    },
    [session, setTeamId],
  );

  const submitJoin = useCallback(
    async (teamCode: string) => {
      if (!session) {
        return;
      }

      await assertPlayerCanJoinOrCreateTeam(session.player.playerId);

      const displayName = `${session.player.firstName} ${session.player.lastName}`;
      const result = await teamService.submitJoinRequest({
        teamCode,
        playerRef: session.player.playerId,
        displayName,
      });
      await setTeamId(null, result.teamId);
      return result;
    },
    [session, setTeamId],
  );

  const processRequest = useCallback(
    async (requestId: string, approve: boolean) => {
      if (!teamId || !playerId) {
        return;
      }

      await teamService.processJoinRequest({
        teamId,
        requestId,
        approve,
        requestorId: playerId,
      });
      await loadTeam();
    },
    [teamId, playerId, loadTeam],
  );

  const renameTeam = useCallback(
    async (newName: string) => {
      if (!teamId || !playerId) {
        return;
      }

      await teamService.updateTeamName({
        teamId,
        newName,
        requestorId: playerId,
      });
      await loadTeam();
    },
    [teamId, playerId, loadTeam],
  );

  const disbandTeam = useCallback(async () => {
    if (!teamId || !playerId) {
      return;
    }

    await teamService.disbandTeam(teamId, playerId);
    await setTeamId(null, null);
  }, [teamId, playerId, setTeamId]);

  const leaveTeam = useCallback(async () => {
    if (!teamId || !playerId) {
      return;
    }

    await teamService.leaveTeam(teamId, playerId);
    await setTeamId(null, null);
  }, [teamId, playerId, setTeamId]);

  const removeMember = useCallback(
    async (memberPlayerId: string) => {
      if (!teamId || !playerId) {
        return;
      }

      await teamService.removeMember({
        teamId,
        playerId: memberPlayerId,
        requestorId: playerId,
      });

      if (samePlayerRef(memberPlayerId, playerId)) {
        await setTeamId(null, null);
      } else {
        await loadTeam();
      }
    },
    [teamId, playerId, setTeamId, loadTeam],
  );

  const currentRole =
    team?.members.find((member) =>
      playerId ? samePlayerRef(member.playerRef, playerId) : false,
    )?.role ?? null;

  return {
    team,
    pendingRequests,
    isLoading,
    error,
    currentRole,
    loadTeam,
    createTeam,
    submitJoin,
    processRequest,
    renameTeam,
    disbandTeam,
    leaveTeam,
    removeMember,
  };
}
