# UMBRAL - Reglas de Negocio y Restricciones

Debes adherirte estrictamente a las siguientes reglas de negocio (RN) durante la implementación del proyecto UMBRAL.
Cualquier lógica o código generado que viole estas reglas se considerará un error crítico.

## Actores y Permisos
1. **Administrador**: Configura misiones, consulta sesiones, mantiene catálogos base.
2. **Operador**: Inicia sesiones, libera pistas, aplica penalizaciones, supervisa la ejecución.
3. **Equipo**: Consulta su tablero, recibe pistas y envía respuestas o evidencias.
4. **Jugador**: Crea equipos y se une a equipos existentes.

## Reglas de Negocio Extraídas
A continuación se listan las restricciones del juego que el backend debe validar antes de procesar comandos:

- **RN-01 (Inmutabilidad en Uso):** No se puede editar ni eliminar una Misión, Etapa, Juego o Pista si existe al menos una Sesión activa (en curso o pausada) vinculada a dicha misión. ●

- **RN-02 (Tipificación Obligatoria):** El sistema solo permite dos tipos de juego: Trivia (basado en opciones/respuestas de texto) y Búsqueda del Tesoro (basado en hallazgo físico/evidencia).

- **RN-03 (Estado de Sesión):** Un equipo solo puede enviar respuestas o notificar hallazgos si la sesión tiene estado Activa. Si está Pausada o Finalizada, el sistema debe rechazar cualquier comando de juego. ●

- **RN-04 (Cierre de Etapa):** Una etapa/juego se considera "Cerrado" cuando queda resuelto: en Trivia, tras el único intento del equipo (correcto o incorrecto); en Búsqueda del Tesoro, cuando el código QR correcto es validado. El sistema no debe aceptar acciones para una etapa ya cerrada. ●

- **RN-05 (Unicidad de Jugador):** Un usuario (Jugador) solo puede estar vinculado a un único Equipo dentro de una misma sesión.

- **RN-06 (No Duplicidad de Pistas):** El sistema debe impedir que un Operador envíe manualmente una pista que el equipo ya haya recibido previamente. ●

- **RN-07 (Liberación de Pistas):** Las pistas del catálogo (creadas por el Administrador) solo se muestran al equipo cuando el Operador las libera manualmente durante la sesión. Solo pueden liberarse pistas del juego (nodo) actual del equipo; no se pueden liberar pistas de juegos ya superados ni de juegos futuros. El jugador nunca ve el catálogo completo de pistas de la misión.

- **RN-08 (Cálculo de Puntaje):** El puntaje total de un equipo es la suma de los puntos obtenidos por juegos completados menos el valor acumulado de las penalizaciones manuales aplicadas por el Operador. ●

- **RN-09 (Criterio de Desempate):** El Ranking se ordena de mayor a menor puntaje. En caso de empate en puntos, el equipo con el menor tiempo total de ejecución (desde el inicio de la sesión hasta el fin de la última etapa) ocupará la posición superior. ●

- **RN-10 (Justificación de Sanción):** Toda penalización aplicada por el Operador debe ir acompañada obligatoriamente de un motivo o descripción. El sistema debe rechazar penalizaciones "en blanco".

- **RN-11 (Progresión Secuencial Estricta):** Los equipos deben recorrer las etapas en el orden secuencial estricto definido en la plantilla de la Misión. El sistema no permite saltarse un juego: solo se avanza a N+1 tras resolver el juego N. En Trivia, resolver incluye fallar el único intento (0 puntos y avance); en Búsqueda del Tesoro, resolver requiere el código correcto. ●

- **RN-12 (Validación Asimétrica de Juegos):** Existen dos mecanismos de avance excluyentes: ○ En Trivia, la validación es automática por el motor del sistema (comparación de la respuesta seleccionada del equipo contra la respuesta correcta configurada); el equipo tiene un solo intento por trivia y, si falla, no suma puntaje y avanza. ○ En Búsqueda del Tesoro, la validación es por código QR; el avance solo se produce cuando el equipo envía el código correspondiente a ese tesoro.

- **RN-13 (Bloqueo de Modificación de Equipo en Juego):** Un jugador no puede abandonar un equipo, ni el creador puede disolverlo, si dicho equipo se encuentra actualmente vinculado a una sesión con estado Activa o Pausada. ●

- **RN-14 (Identidad Única de Equipo):** Para garantizar la integridad del ranking y la trazabilidad, no pueden existir dos equipos con el mismo nombre.

- **RN-15 (Condición de Inicio de la Sesión):** Una sesión no puede iniciar su cronómetro ni pasar a estado Activa si no cuenta con al menos un (1) equipo con solicitud de unión formalmente aprobada por el Operador. ●

- **RN-16 (Restricción de Acceso Operativo):** Un Operador únicamente puede visualizar solicitudes de unión, crear sesiones y monitorear misiones que le hayan sido explícitamente asignadas por un Administrador. El sistema no debe permitir el acceso a cualquier otra misión. ●

- **RN-17 (Inmutabilidad Post-Cierre):** Una vez que el Operador o el sistema marca una sesión como Finalizada, ningún puntaje, tiempo o estado puede ser alterado. El registro pasa a ser de solo lectura para la Auditoría.

- **RN-18 (Mensajería Condicionada):** El Operador no puede enviar mensajes de soporte personalizados a un equipo que haya sido expulsado o que ya haya finalizado la misión.
