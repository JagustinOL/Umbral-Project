export type TriviaFeedback = {
  tone: 'success' | 'error';
  title: string;
  message: string;
  nodeCompleted: boolean;
};

export function feedbackFromTriviaResult(result: {
  isCorrect: boolean;
  nodeCompleted: boolean;
  awardedPoints: number;
}): TriviaFeedback {
  if (result.isCorrect && result.nodeCompleted) {
    return {
      tone: 'success',
      title: '¡Trivia completada!',
      message: `Etapa superada. +${result.awardedPoints} pts.`,
      nodeCompleted: true,
    };
  }

  if (result.isCorrect) {
    return {
      tone: 'success',
      title: '¡Correcto!',
      message: 'Continúa con la siguiente pregunta.',
      nodeCompleted: false,
    };
  }

  if (result.nodeCompleted) {
    return {
      tone: 'error',
      title: 'Incorrecto',
      message: 'Sin puntos. Avanzas a la siguiente etapa.',
      nodeCompleted: true,
    };
  }

  return {
    tone: 'error',
    title: 'Incorrecto',
    message: 'Respuesta registrada.',
    nodeCompleted: false,
  };
}
