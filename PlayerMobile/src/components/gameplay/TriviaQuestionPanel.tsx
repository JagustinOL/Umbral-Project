import { StyleSheet, Text, View } from 'react-native';
import { PrimaryButton } from '../PrimaryButton';
import { colors, typography } from '../../constants/theme';
import type { PlayerTriviaQuestion } from '../../types/gameplay';

type TriviaQuestionPanelProps = {
  question: PlayerTriviaQuestion;
  questionIndex: number;
  totalQuestions: number;
  selectedOption: string | null;
  loading: boolean;
  canSubmit: boolean;
  onSelectOption: (option: string) => void;
  onSubmit: () => void;
};

export function TriviaQuestionPanel({
  question,
  questionIndex,
  totalQuestions,
  selectedOption,
  loading,
  canSubmit,
  onSelectOption,
  onSubmit,
}: TriviaQuestionPanelProps) {
  return (
    <View style={styles.section}>
      <Text style={styles.progress}>
        Pregunta {questionIndex + 1} de {totalQuestions}
      </Text>
      <Text style={styles.prompt}>{question.prompt}</Text>
      {question.options.map((option) => {
        const isSelected = selectedOption === option;
        return (
          <View key={option} style={styles.optionWrap}>
            <PrimaryButton
              label={option}
              variant={isSelected ? 'primary' : 'ghost'}
              locked={!canSubmit}
              onPress={() => onSelectOption(option)}
            />
          </View>
        );
      })}
      <View style={styles.spacer} />
      <PrimaryButton
        label="Confirmar respuesta"
        loading={loading}
        locked={!canSubmit || !selectedOption}
        onPress={onSubmit}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  section: {
    marginTop: 16,
  },
  progress: {
    color: colors.accent,
    fontSize: typography.caption,
    fontWeight: '700',
    letterSpacing: 0.6,
    marginBottom: 10,
    textTransform: 'uppercase',
  },
  prompt: {
    color: colors.text,
    fontSize: typography.subtitle,
    fontWeight: '600',
    lineHeight: 24,
    marginBottom: 14,
  },
  optionWrap: {
    marginBottom: 8,
  },
  spacer: {
    height: 8,
  },
});
