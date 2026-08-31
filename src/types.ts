export type FishingDifficulty = 'easy' | 'medium' | 'hard';

export interface DifficultyReward {
  difficulty: FishingDifficulty;
  name: string;
  gold: number;
  stones: number;
  scrolls: number;
  potionReward?: string;
  zone4WidthPercent: number; // width of top zone 4
  speedMultiplier: number;
}

export interface CollectionLevelTask {
  levelNumber: number; // 1 to 5
  title: string;
  targetItemName: string;
  targetItemId: string;
  requiredCount: number;
  rewardDescription: string;
  hint: string;
  easyAdvice: string;
  mediumAdvice: string;
  hardAdvice: string;
}

export interface LootItem {
  id: string;
  name: string;
  category: 'trash' | 'common' | 'rare' | 'epic' | 'legendary' | 'mythic' | 'dragon';
  xp: number;
  description: string;
  color: string;
  iconName: string;
  imageKey?: string;
}

export interface DropCondition {
  quality: string;
  condition: string;
  pool: { item: LootItem; chance: number }[];
}

export interface CSharpScript {
  name: string;
  category: 'Minigames' | 'Core' | 'UI' | 'Systems';
  description: string;
  code: string;
}

export type ActiveTab = 'fishing_sim' | 'mouse_sim' | 'hierarchy_guide' | 'drop_table' | 'scripts' | 'dialogues' | 'assets';


