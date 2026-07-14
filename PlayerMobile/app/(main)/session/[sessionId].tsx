import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import {
  ActivityIndicator,
  RefreshControl,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { ConnectionIndicator } from '../../../src/components/ConnectionIndicator';
import { FinalSummaryPanel } from '../../../src/components/gameplay/FinalSummaryPanel';
import { GameplayTabBar } from '../../../src/components/gameplay/GameplayTabBar';
import { GameplayFeedbackBanner } from '../../../src/components/gameplay/GameplayFeedbackBanner';
import { HintsPanel } from '../../../src/components/gameplay/HintsPanel';
import { PenaltiesPanel } from '../../../src/components/gameplay/PenaltiesPanel';
import { PlayPanel } from '../../../src/components/gameplay/PlayPanel';
import { RankingPanel } from '../../../src/components/gameplay/RankingPanel';
import { InvestigationBackground } from '../../../src/components/InvestigationBackground';
import { PrimaryButton } from '../../../src/components/PrimaryButton';
import { SessionStatusBanner } from '../../../src/components/SessionStatusBanner';
import { colors, typography } from '../../../src/constants/theme';
import { useAuth } from '../../../src/hooks/useAuth';
import { useLiveSessionGameplay } from '../../../src/hooks/useLiveSessionGameplay';
import type { GameplayTab } from '../../../src/types/gameplay';

export default function LiveSessionScreen() {
  const { sessionId } = useLocalSearchParams<{ sessionId: string }>();
  const { session } = useAuth();
  const teamId = session?.teamId;
  const [activeTab, setActiveTab] = useState<GameplayTab>('play');

  const gameplay = useLiveSessionGameplay({
    sessionId: sessionId ?? '',
    teamId: teamId ?? '',
    enabled: Boolean(sessionId && teamId),
  });

  useEffect(() => {
    if (!teamId) {
      router.replace('/(main)/(tabs)/team');
    }
  }, [teamId]);

  useEffect(() => {
    if (gameplay.isTerminal) {
      setActiveTab('summary');
    }
  }, [gameplay.isTerminal]);

  if (!sessionId || !teamId) {
    return null;
  }

  return (
    <InvestigationBackground>
      <ScrollView
        contentContainerStyle={styles.scroll}
        refreshControl={
          <RefreshControl
            refreshing={gameplay.isLoading}
            onRefresh={gameplay.refreshAll}
            tintColor={colors.primary}
          />
        }
      >
        <View style={styles.header}>
          <Text style={styles.eyebrow}>MISIÓN EN VIVO</Text>
          <Text style={styles.title}>Centro de juego</Text>
          <Text style={styles.meta}>
            Session {sessionId.slice(0, 8)}… · Team {teamId.slice(0, 8)}…
          </Text>
        </View>

        <ConnectionIndicator state={gameplay.connectionState} />
        <SessionStatusBanner status={gameplay.sessionStatus} />
        {gameplay.supportMessage ? (
          <GameplayFeedbackBanner
            tone="info"
            title="Mensaje del operador"
            message={gameplay.supportMessage}
          />
        ) : null}

        <GameplayTabBar
          active={activeTab}
          onChange={setActiveTab}
          showSummary={gameplay.isTerminal}
        />

        {gameplay.isLoading && !gameplay.stage ? (
          <ActivityIndicator color={colors.primary} size="large" />
        ) : null}

        {activeTab === 'play' ? (
          <PlayPanel
            sessionId={sessionId}
            teamId={teamId}
            stage={gameplay.stage}
            canSubmit={gameplay.isPlayable}
            onSubmitted={gameplay.refreshStage}
          />
        ) : null}

        {activeTab === 'hints' ? <HintsPanel hints={gameplay.hints} /> : null}
        {activeTab === 'ranking' ? (
          <RankingPanel ranking={gameplay.ranking} />
        ) : null}
        {activeTab === 'penalties' ? (
          <PenaltiesPanel
            penalties={gameplay.penalties}
            lastAlert={gameplay.lastPenaltyAlert}
          />
        ) : null}
        {activeTab === 'summary' ? (
          <FinalSummaryPanel
            sessionId={sessionId}
            teamId={teamId}
            sessionStatus={gameplay.sessionStatus}
          />
        ) : null}

        <View style={styles.footer}>
          <PrimaryButton
            label="Volver a sesiones"
            variant="ghost"
            onPress={() => router.back()}
          />
        </View>
      </ScrollView>
    </InvestigationBackground>
  );
}

const styles = StyleSheet.create({
  scroll: { paddingBottom: 48 },
  header: { marginBottom: 16 },
  eyebrow: {
    color: colors.accent,
    fontSize: typography.caption,
    fontWeight: '700',
    letterSpacing: 1,
    marginBottom: 4,
  },
  title: {
    color: colors.text,
    fontSize: typography.title,
    fontWeight: '700',
    marginBottom: 4,
  },
  meta: {
    color: colors.textMuted,
    fontSize: typography.caption,
  },
  footer: { marginTop: 24 },
});
