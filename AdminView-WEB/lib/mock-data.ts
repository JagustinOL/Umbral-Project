import { Mission, Operator } from "./types";

export const MOCK_MISSIONS: Mission[] = [
  {
    id: "m-001",
    title: "Operation: Silent Cipher",
    description: "A cryptography-based mission requiring teams to decode hidden messages across the city.",
    difficulty: 3,
    maxDurationMinutes: 90,
    status: "Active",
    assignedOperators: [{ operatorId: "op-001" }, { operatorId: "op-002" }],
    nodes: [
      {
        id: "n-001",
        missionId: "m-001",
        type: "Stage",
        title: "Stage 1: The Library",
        description: "First contact point at the central library.",
        executionOrder: 1,
        hints: [
          { id: "h-001", nodeId: "n-001", content: "Look near the reference section." },
        ],
        children: [
          {
            id: "n-002",
            missionId: "m-001",
            parentNodeId: "n-001",
            type: "Trivia",
            title: "Cipher Knowledge Check",
            description: "Test your cryptography knowledge.",
            executionOrder: 1,
            questions: [
              {
                id: "q-001",
                questionText: "Which cipher was used in WWII by the Enigma machine?",
                options: [
                  { id: "o-001", text: "Caesar Cipher", isCorrect: false },
                  { id: "o-002", text: "Rotor Cipher", isCorrect: true },
                  { id: "o-003", text: "Vigenère Cipher", isCorrect: false },
                  { id: "o-004", text: "Playfair Cipher", isCorrect: false },
                ],
              },
            ],
          },
        ],
      },
      {
        id: "n-003",
        missionId: "m-001",
        type: "Stage",
        title: "Stage 2: The Clock Tower",
        description: "Rendezvous at the historical clock tower.",
        executionOrder: 2,
        hints: [],
        children: [
          {
            id: "n-004",
            missionId: "m-001",
            parentNodeId: "n-003",
            type: "TreasureHunt",
            title: "The Hidden Keystone",
            description: "Find the keystone hidden near the tower base.",
            executionOrder: 1,
            instructions: "Scan the QR code embedded in the cornerstone facing north.",
            secretCode: "UMBRAL-TK-7741",
            destination: { latitude: 40.7128, longitude: -74.006 },
          },
        ],
      },
    ],
  },
  {
    id: "m-002",
    title: "Urban Legends: Echo District",
    description: "Investigate paranormal reports in the Echo District through a series of trivia challenges.",
    difficulty: 2,
    maxDurationMinutes: 60,
    status: "Draft",
    assignedOperators: [],
    nodes: [
      {
        id: "n-005",
        missionId: "m-002",
        type: "Stage",
        title: "Stage 1: The Abandoned Warehouse",
        description: "Starting point of the investigation.",
        executionOrder: 1,
        hints: [
          { id: "h-002", nodeId: "n-005", content: "Check the old water tower." },
        ],
        children: [],
      },
    ],
  },
  {
    id: "m-003",
    title: "Neon Runners",
    description: "A high-speed city race with treasure hunt checkpoints and trivia gates.",
    difficulty: 4,
    maxDurationMinutes: 120,
    status: "Inactive",
    assignedOperators: [{ operatorId: "op-003" }],
    nodes: [],
  },
  {
    id: "m-004",
    title: "Project: Starfall",
    description: "Classified reconnaissance mission. All details are on a need-to-know basis.",
    difficulty: 5,
    status: "Draft",
    assignedOperators: [],
    nodes: [],
  },
];

export const MOCK_OPERATORS: Operator[] = [
  {
    id: "op-001",
    firstName: "Carlos",
    lastName: "Mendoza",
    email: "c.mendoza@umbral.ops",
    status: "Active",
    assignedMissions: ["m-001"],
  },
  {
    id: "op-002",
    firstName: "Sara",
    lastName: "Voss",
    email: "s.voss@umbral.ops",
    status: "Active",
    assignedMissions: ["m-001"],
  },
  {
    id: "op-003",
    firstName: "Derek",
    lastName: "Hale",
    email: "d.hale@umbral.ops",
    status: "Active",
    assignedMissions: ["m-003"],
  },
  {
    id: "op-004",
    firstName: "Nadia",
    lastName: "Reyes",
    email: "n.reyes@umbral.ops",
    status: "Inactive",
    assignedMissions: [],
  },
];
