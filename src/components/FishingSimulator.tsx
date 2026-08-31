import React, { useState, useEffect, useRef } from 'react';
import { motion, AnimatePresence } from 'motion/react';
import { 
  Sparkles, 
  RotateCcw, 
  Award, 
  ChevronRight, 
  CheckCircle2, 
  Trophy, 
  Flame, 
  Zap, 
  Volume2, 
  VolumeX,
  Fish,
  Info,
  X,
  ArrowLeft,
  Coins,
  Gem,
  Scroll,
  ShieldAlert,
  HelpCircle,
  BookOpen,
  Target,
  Layers,
  Percent
} from 'lucide-react';
import { LOOT_ITEMS, FISHING_DIFFICULTIES, FISHING_COLLECTION_LEVELS, DROP_CONDITIONS } from '../data/fishingData';
import { LootItem, FishingDifficulty, DifficultyReward } from '../types';

export const FishingSimulator: React.FC = () => {
  // Navigation & Selection
  const [selectedDifficulty, setSelectedDifficulty] = useState<FishingDifficulty | null>(null);
  const [showInfoModal, setShowInfoModal] = useState<boolean>(false);
  const [infoTab, setInfoTab] = useState<'mechanics' | 'chances' | 'levels' | 'diffs'>('mechanics');
  
  // Game session states
  const [currentAttempt, setCurrentAttempt] = useState<number>(1);
  const maxAttempts = 10;
  const [sessionLoot, setSessionLoot] = useState<LootItem[]>([]);
  const [isSessionComplete, setIsSessionComplete] = useState<boolean>(false);


  // Active attempt phase: 'idle' | 'vertical_casting' | 'horizontal_catching' | 'splashing' | 'single_toast'
  const [phase, setPhase] = useState<'idle' | 'vertical_casting' | 'horizontal_catching' | 'splashing' | 'single_toast'>('idle');
  const [verticalPos, setVerticalPos] = useState<number>(0.5); // 0 (bottom) to 1 (top)
  const [horizontalSpread, setHorizontalSpread] = useState<number>(0); // 0 (center) to 1 (outer ends)
  const [soundEnabled, setSoundEnabled] = useState<boolean>(true);
  
  // Single catch details
  const [currentCaughtItem, setCurrentCaughtItem] = useState<LootItem | null>(null);
  const [totalSessionXp, setTotalSessionXp] = useState<number>(0);
  const [verticalLockedVal, setVerticalLockedVal] = useState<number>(0);
  const [horizontalLockedVal, setHorizontalLockedVal] = useState<number>(0);
  const [rodSwinging, setRodSwinging] = useState<boolean>(false);
  const [bobberPos, setBobberPos] = useState<{ x: number; y: number } | null>(null);

  // Animation frame loop refs
  const vertDirRef拼 = useRef<number>(1);
  const horizDirRef = useRef<number>(1);
  const animFrameRef = useRef<number | null>(null);

  const diffConfig: DifficultyReward = selectedDifficulty 
    ? FISHING_DIFFICULTIES[selectedDifficulty] 
    : FISHING_DIFFICULTIES.medium;

  // 1. Vertical Bar loop (Moving indicator line up and down)
  useEffect(() => {
    if (phase !== 'vertical_casting') return;

    let lastTime = performance.now();
    const speed = 1.35 * diffConfig.speedMultiplier;

    const updateLoop = (now: number) => {
      const delta = (now - lastTime) / 1000;
      lastTime = now;

      setVerticalPos(prev => {
        let next = prev + vertDirRef拼.current * speed * delta;
        if (next >= 1) {
          next = 1;
          vertDirRef拼.current = -1;
        } else if (next <= 0) {
          next = 0;
          vertDirRef拼.current = 1;
        }
        return next;
      });

      animFrameRef.current = requestAnimationFrame(updateLoop);
    };

    animFrameRef.current = requestAnimationFrame(updateLoop);
    return () => {
      if (animFrameRef.current) cancelAnimationFrame(animFrameRef.current);
    };
  }, [phase, diffConfig.speedMultiplier]);

  // 2. Horizontal Bar loop (Dual beams moving from center to outer ends and back)
  useEffect(() => {
    if (phase !== 'horizontal_catching') return;

    let lastTime = performance.now();
    const speed = 1.6 * diffConfig.speedMultiplier;

    const updateLoop而去 = (now: number) => {
      const delta而去 = (now - lastTime) / 1000;
      lastTime = now;

      setHorizontalSpread(prev => {
        let next = prev + horizDirRef.current * speed * delta而去;
        if (next >= 1) {
          next = 1;
          horizDirRef.current = -1;
        } else if (next <= 0) {
          next = 0;
          horizDirRef.current = 1;
        }
        return next;
      });

      animFrameRef.current = requestAnimationFrame(updateLoop而去);
    };

    animFrameRef.current = requestAnimationFrame(updateLoop而去);
    return () => {
      if (animFrameRef.current) cancelAnimationFrame(animFrameRef.current);
    };
  }, [phase, diffConfig.speedMultiplier]);

  // Start difficulty
  const handleSelectDifficulty = (diff: FishingDifficulty) => {
    setSelectedDifficulty(diff);
    setCurrentAttempt(1);
    setSessionLoot([]);
    setIsSessionComplete(false);
    setTotalSessionXp(0);
    setPhase('idle');
    setBobberPos(null);
    setCurrentCaughtItem(null);
  };

  // Exit back to difficulty select
  const handleExitToSelect = () => {
    setSelectedDifficulty(null);
    setPhase('idle');
  };

  // Handle Primary Click (Fishing Rod or Big Action Button)
  const handlePrimaryClick = () => {
    if (phase === 'idle') {
      // Step 1: Click rod -> Rod blocked/disabled -> Start Vertical Cast Bar
      setPhase('vertical_casting');
      setBobberPos(null);
      setCurrentCaughtItem(null);
    } else if (phase === 'vertical_casting') {
      // Step 2: Lock Vertical Bar, Swing Rod, Launch Bobber, Hide Vertical Bar -> Open Horizontal Bar
      const lockedV = verticalPos;
      setVerticalLockedVal(lockedV);
      setRodSwinging(true);

      setTimeout(() => setRodSwinging(false), 450);
      setBobberPos({ x: 50 + (lockedV - 0.5) * 20, y: 55 - lockedV * 25 });

      setPhase('horizontal_catching');
      setHorizontalSpread(0);
    } else if (phase === 'horizontal_catching') {
      // Step 3: Lock Horizontal Bar (End-target: player wants rays at outer ends near 1.0)
      const lockedH = horizontalSpread;
      setHorizontalLockedVal(lockedH);
      setPhase('splashing');

      setTimeout(() => {
        resolveCatch(verticalLockedVal, lockedH);
      }, 700);
    }
  };

  // Calculate loot based on sectors and accuracy
  const resolveCatch = (vVal: number, hVal: number) => {
    // Determine Vertical Zone [1, 2, 3, 4]
    // Zone 4 threshold is top (1 - zone4WidthPercent/100)
    const z4Threshold = 1 - (diffConfig.zone4WidthPercent / 100);
    let zoneNumber = 1;
    if (vVal >= z4Threshold) zoneNumber = 4;
    else if (vVal >= 0.50) zoneNumber = 3;
    else if (vVal >= 0.25) zoneNumber = 2;
    else zoneNumber = 1;

    // Horizontal accuracy: player wants rays at outer ends (hVal close to 1.0)
    const edgeAccuracy = hVal; // 0 at center, 1 at ends

    // Combined score
    const combinedScore = (zoneNumber / 4) * 0.6 + edgeAccuracy * 0.4;
    const roll = Math.random();

    let caught: LootItem;

    if (zoneNumber === 4 && edgeAccuracy > 0.75) {
      // Ideal Long Cast with max edge timing!
      if (roll < 0.08) {
        caught珍惜 = LOOT_ITEMS.potion3000; // DRAGON POTION!
      } else if (roll < 0.25) {
        caught珍惜 = LOOT_ITEMS.potion1000;
      } else if (roll < 0.55) {
        caught珍惜 = LOOT_ITEMS.potion500;
      } else {
        caught珍惜 = LOOT_ITEMS.potion300;
      }
    } else if (combinedScore > 0.65) {
      if (roll < 0.20) {
        caught珍惜 = LOOT_ITEMS.potion500;
      } else if (roll < 0.50) {
        caught珍惜 = LOOT_ITEMS.potion300;
      } else if (roll < 0.80) {
        caught珍惜 = LOOT_ITEMS.potion100;
      } else {
        caught珍惜 = LOOT_ITEMS.potion50;
      }
    } else if (combinedScore > 0.35) {
      if (roll < 0.35) {
        caught珍惜 = LOOT_ITEMS.potion100;
      } else if (roll < 0.65) {
        caught珍惜 = LOOT_ITEMS.potion50;
      } else if (roll < 0.85) {
        caught珍惜 = LOOT_ITEMS.potion10;
      } else {
        caught珍惜 = LOOT_ITEMS.runeStone;
      }
    } else {
      // Bad / Sector 1 / Low timing
      if (roll < 0.4) {
        caught珍惜 = LOOT_ITEMS.trashBottle;
      } else if (roll < 0.75) {
        caught珍惜 = LOOT_ITEMS.duckweed;
      } else {
        caught珍惜 = LOOT_ITEMS.potion10;
      }
    }

    const caughtFinal = caught珍惜;
    setCurrentCaughtItem(caughtFinal);
    setTotalSessionXp(prev => prev + caughtFinal.xp);
    setSessionLoot(prev => [...prev, caughtFinal]);
    setPhase('single_toast');
  };

  let caught珍惜: LootItem = LOOT_ITEMS.duckweed;

  const handleNextAttemptOrFinish = () => {
    if (currentAttempt >= maxAttempts) {
      // 10 attempts done! Show final summary popup
      setIsSessionComplete(true);
    } else {
      // Next attempt
      setCurrentAttempt(prev => prev + 1);
      setPhase('idle');
      setBobberPos(null);
      setCurrentCaughtItem(null);
    }
  };

  const handleClaimAllAndFinish = () => {
    // Reset back to difficulty menu and show success
    setSelectedDifficulty(null);
    setIsSessionComplete(false);
    setSessionLoot([]);
  };

  // Calculate sector boundaries for the visual vertical bar
  const z4Width = diffConfig.zone4WidthPercent;
  const z3Width = 35;
  const z2Width = 25;
  const z1Width = 100 - z4Width - z3Width - z2Width;

  return (
    <div className="flex flex-col gap-6 w-full max-w-6xl mx-auto">
      {/* 1. SCREEN: Difficulty Selector (Before Fishing) */}
      {!selectedDifficulty && (
        <div className="bg-slate-900/90 border border-amber-500/30 rounded-3xl p-6 md:p-8 shadow-2xl flex flex-col gap-6">
          <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4 border-b border-slate-800 pb-6">
            <div className="flex items-center gap-3.5">
              <div className="w-12 h-12 rounded-2xl bg-amber-500/20 border border-amber-500/40 flex items-center justify-center text-amber-400 text-2xl shadow-lg">
                🎣
              </div>
              <div>
                <h2 className="text-xl md:text-2xl font-black text-white">Алхимическая Рыбалка: Выбор сложности</h2>
                <p className="text-xs md:text-sm text-slate-400 mt-0.5">
                  10 попыток за сессию без ограничения времени. Чем выше сложность — тем ценнее гарантированные награды!
                </p>
              </div>
            </div>

            <div className="flex items-center gap-3">
              <button
                onClick={() => setShowInfoModal(true)}
                className="flex items-center gap-2 bg-gradient-to-r from-amber-500/20 to-amber-600/20 hover:from-amber-500/30 hover:to-amber-600/30 border border-amber-500/50 px-4 py-2 rounded-xl text-xs text-amber-300 font-bold transition shadow-md hover:scale-105"
              >
                <HelpCircle className="w-4 h-4 text-amber-400 animate-pulse" />
                <span>Шансы дропа и Справка</span>
              </button>

              <div className="hidden sm:flex items-center gap-2 bg-amber-950/40 border border-amber-500/30 px-3.5 py-2 rounded-xl text-xs text-amber-300 font-semibold">
                <Flame className="w-4 h-4 text-amber-400" />
                <span>Драконье Зелье (+3000 XP)</span>
              </div>
            </div>
          </div>


          {/* 3 Difficulty Cards */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
            {/* EASY */}
            <div className="bg-slate-950/80 border-2 border-emerald-500/40 hover:border-emerald-400 rounded-2xl p-5 flex flex-col justify-between gap-5 transition group hover:shadow-xl hover:shadow-emerald-950/50">
              <div className="flex flex-col gap-3">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-black text-emerald-400 uppercase tracking-wider bg-emerald-950 px-2.5 py-1 rounded-lg border border-emerald-500/30">
                    Легкий уровень
                  </span>
                  <span className="text-lg">🌿</span>
                </div>

                <div>
                  <h3 className="text-lg font-bold text-white group-hover:text-emerald-300 transition">Тихий залив</h3>
                  <p className="text-xs text-slate-400 mt-1">
                    Широкая зона 4 (35%) и плавная скорость шкал. Идеально для спокойной ловли.
                  </p>
                </div>

                <div className="bg-slate-900/90 rounded-xl p-3 border border-slate-800 space-y-2 text-xs">
                  <div className="text-[11px] font-bold text-slate-400 uppercase">Гарантированные награды:</div>
                  <div className="flex items-center justify-between text-amber-300 font-semibold">
                    <span className="flex items-center gap-1.5"><Coins className="w-3.5 h-3.5" /> Золото:</span>
                    <span>3 000</span>
                  </div>
                  <div className="flex items-center justify-between text-blue-300 font-semibold">
                    <span className="flex items-center gap-1.5"><Gem className="w-3.5 h-3.5" /> Камни:</span>
                    <span>3 шт</span>
                  </div>
                  <div className="flex items-center justify-between text-purple-300 font-semibold">
                    <span className="flex items-center gap-1.5"><Scroll className="w-3.5 h-3.5" /> Свитки:</span>
                    <span>1 шт</span>
                  </div>
                </div>
              </div>

              <button
                onClick={() => handleSelectDifficulty('easy')}
                className="w-full py-3 bg-emerald-500 hover:bg-emerald-400 text-slate-950 font-black text-xs uppercase tracking-wider rounded-xl transition shadow-lg flex items-center justify-center gap-2"
              >
                <span>Выбрать Легкий (10 попыток)</span>
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>

            {/* MEDIUM */}
            <div className="bg-slate-950/80 border-2 border-blue-500/50 hover:border-blue-400 rounded-2xl p-5 flex flex-col justify-between gap-5 transition group hover:shadow-xl hover:shadow-blue-950/50 relative">
              <div className="absolute -top-3 right-4 bg-blue-500 text-slate-950 text-[10px] font-black uppercase px-2.5 py-0.5 rounded-full shadow">
                Популярный
              </div>

              <div className="flex flex-col gap-3">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-black text-blue-400 uppercase tracking-wider bg-blue-950 px-2.5 py-1 rounded-lg border border-blue-500/30">
                    Средний уровень
                  </span>
                  <span className="text-lg">🌊</span>
                </div>

                <div>
                  <h3 className="text-lg font-bold text-white group-hover:text-blue-300 transition">Бурный исток</h3>
                  <p className="text-xs text-slate-400 mt-1">
                    Средняя зона 4 (22%) и повышенная скорость. Сбалансированный риск и награды.
                  </p>
                </div>

                <div className="bg-slate-900/90 rounded-xl p-3 border border-slate-800 space-y-2 text-xs">
                  <div className="text-[11px] font-bold text-slate-400 uppercase">Гарантированные награды:</div>
                  <div className="flex items-center justify-between text-amber-300 font-semibold">
                    <span className="flex items-center gap-1.5"><Coins className="w-3.5 h-3.5" /> Золото:</span>
                    <span>5 000</span>
                  </div>
                  <div className="flex items-center justify-between text-blue-300 font-semibold">
                    <span className="flex items-center gap-1.5"><Gem className="w-3.5 h-3.5" /> Камни:</span>
                    <span>5 шт</span>
                  </div>
                  <div className="flex items-center justify-between text-purple-300 font-semibold">
                    <span className="flex items-center gap-1.5"><Scroll className="w-3.5 h-3.5" /> Свитки:</span>
                    <span>2 шт</span>
                  </div>
                </div>
              </div>

              <button
                onClick={() => handleSelectDifficulty('medium')}
                className="w-full py-3 bg-blue-500 hover:bg-blue-400 text-slate-950 font-black text-xs uppercase tracking-wider rounded-xl transition shadow-lg flex items-center justify-center gap-2"
              >
                <span>Выбрать Средний (10 попыток)</span>
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>

            {/* HARD */}
            <div className="bg-slate-950/80 border-2 border-amber-500/60 hover:border-amber-400 rounded-2xl p-5 flex flex-col justify-between gap-5 transition group hover:shadow-xl hover:shadow-amber-950/60 relative">
              <div className="absolute -top-3 right-4 bg-amber-400 text-slate-950 text-[10px] font-black uppercase px-2.5 py-0.5 rounded-full shadow flex items-center gap-1">
                <Flame className="w-3 h-3" />
                <span>Макс. Награды</span>
              </div>

              <div className="flex flex-col gap-3">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-black text-amber-400 uppercase tracking-wider bg-amber-950 px-2.5 py-1 rounded-lg border border-amber-500/30">
                    Сложный уровень
                  </span>
                  <span className="text-lg">🐉</span>
                </div>

                <div>
                  <h3 className="text-lg font-bold text-white group-hover:text-amber-300 transition">Омут Дракона</h3>
                  <p className="text-xs text-slate-400 mt-1">
                    Узкая зона 4 (12%) и высокая скорость. Требует точнейшей реакции!
                  </p>
                </div>

                <div className="bg-slate-900/90 rounded-xl p-3 border border-slate-800 space-y-2 text-xs">
                  <div className="text-[11px] font-bold text-slate-400 uppercase">Гарантированные награды:</div>
                  <div className="flex items-center justify-between text-amber-300 font-semibold">
                    <span className="flex items-center gap-1.5"><Coins className="w-3.5 h-3.5" /> Золото:</span>
                    <span>10 000</span>
                  </div>
                  <div className="flex items-center justify-between text-blue-300 font-semibold">
                    <span className="flex items-center gap-1.5"><Gem className="w-3.5 h-3.5" /> Камни:</span>
                    <span>10 шт</span>
                  </div>
                  <div className="flex items-center justify-between text-purple-300 font-semibold">
                    <span className="flex items-center gap-1.5"><Scroll className="w-3.5 h-3.5" /> Свитки:</span>
                    <span>5 шт</span>
                  </div>
                  <div className="flex items-center justify-between text-amber-400 font-bold bg-amber-950/60 px-2 py-1 rounded border border-amber-500/40">
                    <span>🧪 Зелье Мастерства:</span>
                    <span>+100 XP</span>
                  </div>
                </div>
              </div>

              <button
                onClick={() => handleSelectDifficulty('hard')}
                className="w-full py-3 bg-gradient-to-r from-amber-500 to-amber-400 hover:from-amber-400 hover:to-amber-300 text-slate-950 font-black text-xs uppercase tracking-wider rounded-xl transition shadow-lg flex items-center justify-center gap-2"
              >
                <span>Выбрать Сложный (10 попыток)</span>
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        </div>
      )}

      {/* 2. SCREEN: Active Fishing Stage (When difficulty selected) */}
      {selectedDifficulty && (
        <div className="flex flex-col lg:flex-row gap-6 w-full">
          {/* Main Stage */}
          <div className="flex-1 bg-slate-900/90 border border-amber-500/30 rounded-3xl p-4 md:p-6 shadow-2xl relative overflow-hidden flex flex-col items-center">
            {/* Top Bar with Close_Button & Attempt Counter */}
            <div className="w-full flex items-center justify-between px-3 py-2 bg-slate-950/80 border border-slate-800 rounded-2xl mb-4 backdrop-blur-md gap-2">
              {/* Left Group: Close & Info Button */}
              <div className="flex items-center gap-2">
                <button
                  onClick={handleExitToSelect}
                  className="flex items-center gap-1.5 px-3 py-1.5 bg-slate-900 hover:bg-rose-950/60 text-slate-300 hover:text-rose-300 rounded-xl border border-slate-800 hover:border-rose-500/40 text-xs font-bold transition"
                  title="Вернуться к выбору сложности"
                >
                  <X className="w-4 h-4 text-rose-400" />
                  <span className="hidden sm:inline">Выйти</span>
                </button>

                {/* Info / Guide Button */}
                <button
                  onClick={() => setShowInfoModal(true)}
                  className="flex items-center gap-1.5 px-3 py-1.5 bg-gradient-to-r from-amber-500/20 to-amber-600/20 hover:from-amber-500/30 hover:to-amber-600/30 text-amber-300 rounded-xl border border-amber-500/50 text-xs font-bold transition shadow-sm hover:scale-105"
                  title="Справка по рыбалке: шансы, полоски и советы кота"
                >
                  <HelpCircle className="w-4 h-4 text-amber-400 animate-pulse" />
                  <span>Шансы и Справка</span>
                </button>
              </div>

              {/* Attempts Badge */}
              <div className="flex items-center gap-2 bg-amber-950/60 border border-amber-500/40 px-3 sm:px-4 py-1.5 rounded-xl text-xs font-extrabold text-amber-300">
                <span className="text-amber-400 hidden sm:inline">ПОПЫТКА:</span>
                <span className="text-white text-sm bg-slate-900 px-2 py-0.5 rounded-md border border-amber-500/40">
                  {currentAttempt} / {maxAttempts}
                </span>
                <span className="text-slate-400 text-[11px] font-normal hidden md:inline">({diffConfig.name})</span>
              </div>

              {/* Accumulated XP */}
              <div className="flex items-center gap-2 text-xs font-bold text-emerald-400 bg-emerald-950/50 border border-emerald-500/30 px-3 py-1.5 rounded-xl">
                <Award className="w-4 h-4 text-emerald-400" />
                <span>+{totalSessionXp} XP</span>
              </div>
            </div>


            {/* Main Pond Stage (Visual Representation of Background_Pond) */}
            <div 
              onClick={phase !== 'single_toast' ? handlePrimaryClick : undefined}
              className="relative w-full h-[430px] rounded-2xl overflow-hidden cursor-pointer select-none border border-teal-900/60 shadow-inner group"
              style={{
                background: 'radial-gradient(ellipse at 50% 60%, #0d3b4c 0%, #082330 50%, #031017 100%)',
              }}
            >
              {/* Animated Pond Water & Reflections */}
              <div className="absolute inset-0 pointer-events-none opacity-60">
                <div className="absolute top-1/4 left-1/3 w-72 h-72 rounded-full bg-teal-400/10 blur-3xl animate-pulse" />
                <div className="absolute top-1/2 right-1/4 w-80 h-80 rounded-full bg-emerald-500/10 blur-3xl" />
                <div className="absolute inset-0 bg-[radial-gradient(circle_at_50%_50%,rgba(16,185,129,0.08)_1px,transparent_1px)] bg-[length:24px_24px]" />
              </div>

              {/* Water Lilies */}
              <div className="absolute top-[35%] left-[28%] text-2xl filter drop-shadow-md select-none pointer-events-none opacity-85">🪷</div>
              <div className="absolute top-[52%] left-[46%] text-3xl filter drop-shadow-md select-none pointer-events-none opacity-90 animate-bounce" style={{ animationDuration: '4s' }}>🪷</div>
              <div className="absolute top-[40%] right-[38%] text-2xl filter drop-shadow-md select-none pointer-events-none opacity-85">🪷</div>

              {/* Bobber In Water */}
              {bobberPos && (
                <motion.div 
                  initial={{ scale: 0, y: -80, opacity: 0 }}
                  animate={{ 
                    scale: 1, 
                    y: 0, 
                    opacity: 1,
                    top: `${bobberPos.y}%`,
                    left: `${bobberPos.x}%`
                  }}
                  transition={{ type: 'spring', damping: 12 }}
                  className="absolute -translate-x-1/2 -translate-y-1/2 pointer-events-none z-20 flex flex-col items-center"
                >
                  <div className="absolute w-12 h-6 border border-teal-300/40 rounded-full animate-ping" />
                  <div className="w-5 h-7 bg-gradient-to-b from-red-500 via-white to-red-600 rounded-full shadow-lg border border-amber-300 flex items-center justify-center">
                    <div className="w-1.5 h-1.5 bg-yellow-300 rounded-full shadow" />
                  </div>
                  <span className="text-[10px] text-teal-200 font-bold bg-slate-900/80 px-1.5 py-0.5 rounded mt-1 shadow border border-teal-500/30">
                    {phase === 'splashing' ? 'ПОДСЕКАЕМ!' : 'В ВОДЕ'}
                  </span>
                </motion.div>
              )}

              {/* Splash Effect */}
              {phase === 'splashing' && (
                <motion.div 
                  initial={{ scale: 0.4, opacity: 0 }}
                  animate={{ scale: 2.4, opacity: 1 }}
                  exit={{ opacity: 0 }}
                  className="absolute top-[48%] left-[50%] -translate-x-1/2 -translate-y-1/2 z-30 pointer-events-none text-4xl text-teal-300 drop-shadow-xl"
                >
                  💦✨
                </motion.div>
              )}

              {/* 1. Vertical Cast Bar (Скриншот 1: справа над удочкой с 3 делителями и 4 секторами) */}
              <AnimatePresence>
                {phase === 'vertical_casting' && (
                  <motion.div 
                    initial={{ opacity: 0, x: 30 }}
                    animate={{ opacity: 1, x: 0 }}
                    exit={{ opacity: 0, scale: 0.8 }}
                    className="absolute right-6 top-6 bottom-20 w-16 flex flex-col items-center bg-slate-950/90 backdrop-blur-md border-2 border-amber-400/80 rounded-2xl p-2 shadow-2xl z-30"
                  >
                    <div className="text-[9px] uppercase font-black text-amber-300 mb-1 tracking-wider text-center">
                      ШКАЛА 1: ДАЛЬНОСТЬ
                    </div>
                    
                    {/* Vertical Slot with 4 Zones and 3 Delimiters */}
                    <div className="relative flex-1 w-8 bg-slate-900 rounded-xl overflow-hidden border border-slate-700 shadow-inner flex flex-col">
                      {/* Zone 4 (Top: Dragon / Supreme loot) */}
                      <div 
                        style={{ height: `${z4Width}%` }}
                        className="w-full bg-red-500/30 border-b-2 border-red-400 flex items-center justify-center relative"
                      >
                        <span className="text-[10px] font-black text-red-300">4</span>
                        <span className="absolute left-0.5 text-[7px] text-red-400 font-mono">МАКС</span>
                      </div>

                      {/* Zone 3 (Mid-High) */}
                      <div 
                        style={{ height: `${z3Width}%` }}
                        className="w-full bg-purple-500/25 border-b-2 border-purple-400 flex items-center justify-center relative"
                      >
                        <span className="text-[10px] font-black text-purple-300">3</span>
                      </div>

                      {/* Zone 2 (Mid-Low) */}
                      <div 
                        style={{ height: `${z2Width}%` }}
                        className="w-full bg-blue-500/25 border-b-2 border-blue-400 flex items-center justify-center relative"
                      >
                        <span className="text-[10px] font-black text-blue-300">2</span>
                      </div>

                      {/* Zone 1 (Bottom: Trash/Low) */}
                      <div 
                        style={{ height: `${z1Width}%` }}
                        className="w-full bg-slate-700/30 flex items-center justify-center relative"
                      >
                        <span className="text-[10px] font-black text-slate-400">1</span>
                        <span className="absolute left-0.5 text-[7px] text-slate-500 font-mono">МИН</span>
                      </div>

                      {/* Moving Arrow / Slider */}
                      <div 
                        className="absolute left-0 right-0 h-3 bg-gradient-to-r from-amber-300 via-yellow-100 to-amber-300 border border-white rounded shadow-[0_0_12px_#fbbf24] transition-all duration-75"
                        style={{ bottom: `${verticalPos * 90}%` }}
                      >
                        <div className="w-full h-0.5 bg-white" />
                      </div>
                    </div>

                    <div className="text-[10px] font-mono text-amber-200 mt-1 font-bold">
                      {Math.round(verticalPos * 100)}%
                    </div>
                  </motion.div>
                )}
              </AnimatePresence>

              {/* 2. Horizontal Catch Bar (Скриншот 2: сверху с 2 расходящимися от центра лучами) */}
              <AnimatePresence>
                {phase === 'horizontal_catching' && (
                  <motion.div 
                    initial={{ opacity: 0, y: -20 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, scale: 0.8 }}
                    className="absolute top-6 left-1/2 -translate-x-1/2 w-[85%] max-w-md bg-slate-950/95 backdrop-blur-md border-2 border-purple-500/90 rounded-2xl p-3 shadow-2xl z-30 flex flex-col items-center"
                  >
                    <div className="text-[11px] font-black text-purple-300 uppercase tracking-wider mb-1 flex items-center gap-1.5">
                      <Sparkles className="w-3.5 h-3.5 text-purple-400" />
                      <span>ШКАЛА 2: ЖМИ, КОГДА ЛУЧИ ДОЙДУТ ДО КОНЦОВ!</span>
                    </div>

                    {/* Outer Frame with Center and Edge markers */}
                    <div className="relative w-full h-9 bg-slate-900 rounded-xl overflow-hidden border border-purple-900 shadow-inner flex items-center">
                      {/* Left Target End */}
                      <div className="absolute left-0 top-0 bottom-0 w-8 bg-emerald-500/30 border-r border-emerald-400 flex items-center justify-center">
                        <span className="text-[8px] font-black text-emerald-300">ЦЕЛЬ</span>
                      </div>

                      {/* Right Target End */}
                      <div className="absolute right-0 top-0 bottom-0 w-8 bg-emerald-500/30 border-l border-emerald-400 flex items-center justify-center">
                        <span className="text-[8px] font-black text-emerald-300">ЦЕЛЬ</span>
                      </div>

                      {/* Center Origin Mark */}
                      <div className="absolute left-1/2 -translate-x-1/2 w-1 h-full bg-purple-400/50" />

                      {/* Dual Expanding Beams (from center to left and right) */}
                      <div 
                        className="absolute top-0 bottom-0 w-3 bg-gradient-to-r from-pink-500 via-purple-200 to-pink-500 border-x border-white shadow-[0_0_15px_#d946ef] rounded-sm transition-all duration-75 -translate-x-1/2"
                        style={{ left: `${50 - horizontalSpread * 46}%` }}
                      />
                      <div 
                        className="absolute top-0 bottom-0 w-3 bg-gradient-to-r from-pink-500 via-purple-200 to-pink-500 border-x border-white shadow-[0_0_15px_#d946ef] rounded-sm transition-all duration-75 -translate-x-1/2"
                        style={{ left: `${50 + horizontalSpread * 46}%` }}
                      />
                    </div>

                    <div className="text-[10px] text-purple-300/80 mt-1 font-medium">
                      Лучи расходятся от центра к краям. Кликните, когда они на краях!
                    </div>
                  </motion.div>
                )}
              </AnimatePresence>

              {/* 3. Fishing Rod Visual (Скриншот 1 [№6] / Скриншот 2 [№2] в правом нижнем углу) */}
              <div 
                className="absolute bottom-2 right-4 z-20 flex flex-col items-center group/rod"
                title="Удочка алхимика"
              >
                {/* Click Hint when Idle */}
                {phase === 'idle' && (
                  <div className="mb-1 flex items-center gap-1.5 px-3 py-1 bg-amber-500 text-slate-950 text-xs font-black rounded-full shadow-lg animate-bounce border border-amber-300">
                    <span>🎣 НАЖМИ НА УДОЧКУ!</span>
                  </div>
                )}

                <motion.div 
                  animate={{ 
                    rotate: rodSwinging ? -22 : phase === 'idle' ? [0, -3, 0] : -8,
                    scale: phase === 'idle' ? 1.05 : 1,
                    filter: phase === 'idle' ? 'brightness(1)' : 'brightness(0.65) grayscale(0.4)'
                  }}
                  transition={{ duration: phase === 'idle' ? 2 : 0.25, repeat: phase === 'idle' ? Infinity : 0 }}
                  className={`w-36 h-36 relative ${phase === 'idle' ? 'cursor-pointer' : 'cursor-not-allowed'}`}
                >
                  {/* Rod graphics */}
                  <div className="absolute bottom-4 right-6 w-44 h-8 bg-gradient-to-r from-amber-700 via-amber-500 to-amber-200 rounded-full rotate-[48deg] shadow-lg border border-amber-300 origin-bottom-left flex items-center justify-end pr-2">
                    <div className="w-4 h-4 rounded-full bg-red-600 border border-amber-200 shadow-[0_0_8px_#ef4444]" />
                  </div>
                  <div className="absolute bottom-8 right-12 w-6 h-6 rounded-full bg-gradient-to-br from-amber-300 to-amber-700 border-2 border-amber-200 shadow" />

                  {/* Blocked indicator when active */}
                  {phase !== 'idle' && (
                    <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 bg-slate-950/80 px-2 py-0.5 rounded text-[9px] font-bold text-amber-400 border border-amber-500/40">
                      ЗАБЛОЧЕНО
                    </div>
                  )}
                </motion.div>
              </div>

              {/* Idle Welcome Overlay */}
              {phase === 'idle' && (
                <div className="absolute inset-0 flex flex-col items-center justify-center pointer-events-none">
                  <div className="bg-slate-950/85 backdrop-blur-md border border-teal-500/40 rounded-2xl px-6 py-4 text-center max-w-sm shadow-2xl">
                    <div className="text-emerald-400 font-bold text-sm uppercase tracking-wider mb-1">
                      Попытка {currentAttempt} из {maxAttempts}
                    </div>
                    <p className="text-xs text-slate-300 leading-relaxed mb-3">
                      Нажмите на удочку в правом углу, чтобы запустить вертикальную шкалу заброса!
                    </p>
                    <div className="inline-flex items-center gap-1.5 text-[11px] text-amber-300 font-semibold bg-amber-950/60 border border-amber-500/30 px-3 py-1 rounded-lg">
                      <Flame className="w-3.5 h-3.5 text-amber-400" />
                      <span>{diffConfig.name} • Зона 4: {diffConfig.zone4WidthPercent}%</span>
                    </div>
                  </div>
                </div>
              )}
            </div>

            {/* Action Button */}
            <div className="w-full mt-4 flex items-center gap-3">
              <button
                onClick={handlePrimaryClick}
                disabled={phase === 'single_toast' || phase === 'splashing'}
                className={`flex-1 py-3.5 px-6 rounded-2xl font-black text-sm tracking-wide transition-all duration-200 shadow-xl flex items-center justify-center gap-2 ${
                  phase === 'idle'
                    ? 'bg-gradient-to-r from-emerald-500 via-teal-500 to-emerald-600 hover:from-emerald-400 hover:to-teal-500 text-white shadow-emerald-900/40'
                    : phase === 'vertical_casting'
                    ? 'bg-gradient-to-r from-amber-500 via-yellow-500 to-amber-600 hover:from-amber-400 hover:to-yellow-400 text-slate-950 shadow-amber-900/40 scale-[1.02]'
                    : phase === 'horizontal_catching'
                    ? 'bg-gradient-to-r from-purple-600 via-pink-600 to-purple-700 hover:from-purple-500 hover:to-pink-500 text-white shadow-purple-900/40 scale-[1.02]'
                    : 'bg-slate-800 text-slate-400'
                }`}
              >
                {phase === 'idle' && (
                  <>
                    <span>🎣 ЗАБРОСИТЬ УДОЧКУ (ПОПЫТКА {currentAttempt}/{maxAttempts})</span>
                    <ChevronRight className="w-4 h-4" />
                  </>
                )}
                {phase === 'vertical_casting' && (
                  <>
                    <Sparkles className="w-4 h-4" />
                    <span>ФИКСИРОВАТЬ ДАЛЬНОСТЬ (ШКАЛА 1)!</span>
                  </>
                )}
                {phase === 'horizontal_catching' && (
                  <>
                    <Zap className="w-4 h-4" />
                    <span>ПОДСЕЧЬ НА КРАЯХ (ШКАЛА 2)!</span>
                  </>
                )}
                {phase === 'splashing' && <span>ТЯНЕМ ТРОФЕЙ... 🌊</span>}
                {phase === 'single_toast' && <span>ПОЙМАНО!</span>}
              </button>
            </div>

            {/* Single Catch Toast Overlay (Between attempts) */}
            <AnimatePresence>
              {phase === 'single_toast' && currentCaughtItem && (
                <motion.div 
                  initial={{ opacity: 0, scale: 0.85 }}
                  animate={{ opacity: 1, scale: 1 }}
                  exit={{ opacity: 0, scale: 0.9 }}
                  className="absolute inset-4 bg-slate-950/95 backdrop-blur-xl border-2 border-amber-500/80 rounded-2xl p-6 shadow-2xl z-40 flex flex-col items-center justify-center text-center"
                >
                  <div className="text-xs font-black text-amber-400 uppercase tracking-widest mb-1">
                    ПОПЫТКА {currentAttempt} ИЗ {maxAttempts}
                  </div>

                  <div 
                    className="w-24 h-24 rounded-2xl flex items-center justify-center text-5xl my-3 shadow-2xl border-2 relative overflow-hidden"
                    style={{ 
                      backgroundColor: `${currentCaughtItem.color}15`,
                      borderColor: currentCaughtItem.color,
                      boxShadow: `0 0 25px ${currentCaughtItem.color}40`
                    }}
                  >
                    {currentCaughtItem.id === 'potion3000' && '🐉'}
                    {currentCaughtItem.id === 'potion1000' && '🌌'}
                    {currentCaughtItem.id === 'potion500' && '👑'}
                    {currentCaughtItem.id === 'potion300' && '🔮'}
                    {currentCaughtItem.id === 'potion100' && '🧪'}
                    {currentCaughtItem.id === 'potion50' && '⚗️'}
                    {currentCaughtItem.id === 'potion10' && '🧪'}
                    {currentCaughtItem.id === 'runeStone' && '💎'}
                    {currentCaughtItem.id === 'trashBottle' && '🍾'}
                    {currentCaughtItem.id === 'duckweed' && '🪨'}
                  </div>

                  <h4 className="text-xl font-black text-white mb-1" style={{ color: currentCaughtItem.color }}>
                    {currentCaughtItem.name}
                  </h4>

                  <p className="text-xs text-slate-300 max-w-md mb-3 leading-relaxed">
                    {currentCaughtItem.description}
                  </p>

                  {currentCaughtItem.xp > 0 && (
                    <div className="bg-emerald-950/80 border border-emerald-500/50 px-4 py-1 rounded-xl text-emerald-300 font-bold text-sm mb-4 shadow">
                      +{currentCaughtItem.xp} XP
                    </div>
                  )}

                  <button
                    onClick={handleNextAttemptOrFinish}
                    className="py-3 px-8 bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-400 hover:to-amber-500 text-slate-950 font-black text-sm rounded-xl shadow-xl transition-transform hover:scale-105"
                  >
                    {currentAttempt >= maxAttempts ? 'ПЕРЕЙТИ К ИТОГАМ РЫБАЛКИ' : `ПРОДОЛЖИТЬ (СЛЕДУЮЩАЯ ПОПЫТКА ${currentAttempt + 1})`}
                  </button>
                </motion.div>
              )}
            </AnimatePresence>

            {/* Final 10 Attempts Summary Popup (Result_Summary_Popup_Panel) */}
            <AnimatePresence>
              {isSessionComplete && (
                <motion.div 
                  initial={{ opacity: 0, scale: 0.9 }}
                  animate={{ opacity: 1, scale: 1 }}
                  exit={{ opacity: 0 }}
                  className="absolute inset-2 bg-slate-950/98 backdrop-blur-2xl border-2 border-amber-400 rounded-3xl p-6 shadow-2xl z-50 flex flex-col justify-between overflow-y-auto"
                >
                  <div className="flex flex-col items-center text-center">
                    <div className="w-12 h-12 rounded-2xl bg-amber-500/20 border border-amber-400/50 flex items-center justify-center text-amber-300 mb-2">
                      <Trophy className="w-7 h-7 text-amber-400" />
                    </div>

                    <h3 className="text-2xl font-black text-white">Рыбалка завершена!</h3>
                    <p className="text-xs text-slate-400 mt-1">
                      Вы использовали все 10 попыток на уровне «{diffConfig.name}». Вот ваш итоговый улов и награды:
                    </p>

                    {/* Guaranteed Difficulty Rewards Card */}
                    <div className="w-full max-w-md bg-gradient-to-r from-amber-950/60 to-slate-900 border border-amber-500/40 rounded-2xl p-3.5 my-3 grid grid-cols-3 gap-2 text-center text-xs">
                      <div className="bg-slate-950/80 p-2 rounded-xl border border-slate-800">
                        <div className="text-slate-400 text-[10px]">Золото</div>
                        <div className="text-amber-300 font-bold text-sm">+{diffConfig.gold.toLocaleString()}</div>
                      </div>
                      <div className="bg-slate-950/80 p-2 rounded-xl border border-slate-800">
                        <div className="text-slate-400 text-[10px]">Камни</div>
                        <div className="text-blue-300 font-bold text-sm">+{diffConfig.stones} шт</div>
                      </div>
                      <div className="bg-slate-950/80 p-2 rounded-xl border border-slate-800">
                        <div className="text-slate-400 text-[10px]">Свитки</div>
                        <div className="text-purple-300 font-bold text-sm">+{diffConfig.scrolls} шт</div>
                      </div>
                    </div>

                    {diffConfig.potionReward && (
                      <div className="text-xs font-bold text-amber-300 bg-amber-950/70 border border-amber-500/40 px-3 py-1 rounded-xl mb-3">
                        🎁 Бонус: {diffConfig.potionReward}
                      </div>
                    )}

                    {/* Stacking Chest Inventory & 10 Caught Items Toggle Tabs */}
                    <div className="w-full max-w-xl">
                      {/* Stacked Inventory Calculation */}
                      {(() => {
                        const itemCounts: { [id: string]: { item: LootItem; count: number } } = {};
                        sessionLoot.forEach(item => {
                          if (!itemCounts[item.id]) {
                            itemCounts[item.id] = { item, count: 1 };
                          } else {
                            itemCounts[item.id].count += 1;
                          }
                        });
                        const stackedItems = Object.values(itemCounts);

                        return (
                          <div className="flex flex-col gap-3">
                            <div className="flex items-center justify-between border-b border-slate-800 pb-2">
                              <div className="text-[11px] font-bold text-amber-300 uppercase tracking-wider flex items-center gap-1.5">
                                <span>📦 Сундук Инвентаря (Стаки одинаковых предметов):</span>
                              </div>
                              <span className="text-[10px] text-slate-400 font-mono">
                                Уникальных слотов: {stackedItems.length} | Всего предметов: 10
                              </span>
                            </div>

                            {/* Stacked Chest Slots Grid */}
                            <div className="grid grid-cols-2 sm:grid-cols-4 gap-2.5">
                              {stackedItems.map((entry, sIdx) => (
                                <div 
                                  key={sIdx}
                                  className="relative bg-slate-900/90 border-2 rounded-xl p-2.5 flex flex-col items-center text-center shadow-lg transition hover:scale-102"
                                  style={{ borderColor: `${entry.item.color}60` }}
                                >
                                  {/* Item Stack Count Badge */}
                                  <div className="absolute -top-2 -right-2 bg-gradient-to-r from-amber-500 to-amber-600 text-slate-950 text-[11px] font-black px-2 py-0.5 rounded-full shadow-md border border-amber-300 flex items-center gap-0.5">
                                    <span>x{entry.count}</span>
                                  </div>

                                  <span className="text-2xl mb-1">
                                    {entry.item.id === 'potion3000' && '🐉'}
                                    {entry.item.id === 'potion1000' && '🌌'}
                                    {entry.item.id === 'potion500' && '👑'}
                                    {entry.item.id === 'potion300' && '🔮'}
                                    {entry.item.id === 'potion100' && '🧪'}
                                    {entry.item.id === 'potion50' && '⚗️'}
                                    {entry.item.id === 'potion10' && '🧪'}
                                    {entry.item.id === 'runeStone' && '💎'}
                                    {entry.item.id === 'trashBottle' && '🍾'}
                                    {entry.item.id === 'duckweed' && '🪨'}
                                  </span>

                                  <span className="font-bold text-white text-xs truncate w-full" style={{ color: entry.item.color }}>
                                    {entry.item.name}
                                  </span>

                                  <div className="flex items-center justify-between w-full mt-1 pt-1 border-t border-slate-800/80 text-[10px]">
                                    <span className="text-slate-400">Сумма XP:</span>
                                    <span className="text-emerald-400 font-bold font-mono">
                                      +{entry.item.xp * entry.count} XP
                                    </span>
                                  </div>
                                </div>
                              ))}
                            </div>
                          </div>
                        );
                      })()}
                    </div>
                  </div>

                  {/* Claim Button */}
                  <div className="mt-4 flex flex-col items-center gap-2">
                    <button
                      onClick={handleClaimAllAndFinish}
                      className="w-full max-w-md py-3.5 bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-400 hover:to-amber-500 text-slate-950 font-black text-sm rounded-2xl shadow-2xl transition transform hover:scale-105 flex items-center justify-center gap-2"
                    >
                      <CheckCircle2 className="w-5 h-5" />
                      <span>ЗАБРАТЬ ВСЁ В РЮКЗАК И ЗАВЕРШИТЬ</span>
                    </button>
                  </div>
                </motion.div>
              )}
            </AnimatePresence>
          </div>

          {/* Right: Live Session Loot Log */}
          <div className="w-full lg:w-80 flex flex-col gap-4">
            <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-4">
              <div className="flex items-center justify-between mb-3">
                <h4 className="text-white font-bold text-sm">Улов текущей сессии</h4>
                <span className="text-xs text-amber-400 font-bold">{sessionLoot.length} / 10</span>
              </div>

              {sessionLoot.length === 0 ? (
                <div className="text-center py-8 text-slate-500 text-xs">
                  Нажмите на удочку чтобы сделать первый заброс!
                </div>
              ) : (
                <div className="flex flex-col gap-2 max-h-72 overflow-y-auto pr-1">
                  {sessionLoot.map((item, idx) => (
                    <div 
                      key={idx}
                      className="flex items-center justify-between p-2 rounded-xl bg-slate-950/70 border border-slate-800/80 text-xs"
                    >
                      <div className="flex items-center gap-2 truncate">
                        <span className="w-2 h-2 rounded-full" style={{ backgroundColor: item.color }} />
                        <span className="text-slate-200 font-medium truncate">{item.name}</span>
                      </div>
                      <span className="text-emerald-400 font-bold shrink-0">
                        +{item.xp} XP
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Guaranteed Difficulty Summary Info */}
            <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-4 flex flex-col gap-2 text-xs">
              <div className="font-bold text-slate-300">Параметры уровня:</div>
              <div className="flex justify-between text-slate-400">
                <span>Сложность:</span>
                <span className="text-amber-300 font-semibold">{diffConfig.name}</span>
              </div>
              <div className="flex justify-between text-slate-400">
                <span>Ширина Зоны 4:</span>
                <span className="text-emerald-300 font-semibold">{diffConfig.zone4WidthPercent}%</span>
              </div>
              <div className="flex justify-between text-slate-400">
                <span>Множитель скорости:</span>
                <span className="text-purple-300 font-semibold">x{diffConfig.speedMultiplier}</span>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* 3. HELP & INFO MODAL (Ярлычок "Шансы и Справка" с подсказками Кота, 5 уровнями и полосками) */}
      <AnimatePresence>
        {showInfoModal && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 z-50 bg-slate-950/85 backdrop-blur-md flex items-center justify-center p-3 sm:p-6"
            onClick={() => setShowInfoModal(false)}
          >
            <motion.div
              initial={{ scale: 0.9, y: 20 }}
              animate={{ scale: 1, y: 0 }}
              exit={{ scale: 0.9, y: 20 }}
              onClick={e => e.stopPropagation()}
              className="bg-slate-900 border-2 border-amber-500/80 rounded-3xl w-full max-w-4xl max-h-[90vh] shadow-2xl flex flex-col overflow-hidden text-slate-100"
            >
              {/* Modal Header */}
              <div className="p-4 sm:p-5 bg-slate-950 border-b border-slate-800 flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 rounded-2xl bg-amber-500/20 border border-amber-400/40 flex items-center justify-center text-amber-300 text-xl shadow">
                    🐱
                  </div>
                  <div>
                    <h3 className="text-lg font-black text-white flex items-center gap-2">
                      <span>Справка и Шансы: Алхимическая Рыбалка</span>
                      <span className="text-[10px] px-2 py-0.5 rounded bg-amber-500/20 text-amber-300 border border-amber-500/30">
                        Советы Кота
                      </span>
                    </h3>
                    <p className="text-xs text-slate-400">
                      Как закидывать удочку, где ловить редкие зелья и как собрать 5 коллекционных уровней
                    </p>
                  </div>
                </div>

                <button
                  onClick={() => setShowInfoModal(false)}
                  className="w-8 h-8 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white flex items-center justify-center transition"
                >
                  <X className="w-5 h-5" />
                </button>
              </div>

              {/* Navigation Sub-Tabs */}
              <div className="flex items-center gap-1.5 p-2.5 bg-slate-950/60 border-b border-slate-800 overflow-x-auto no-scrollbar">
                <button
                  onClick={() => setInfoTab('mechanics')}
                  className={`px-3.5 py-1.5 rounded-xl text-xs font-bold transition flex items-center gap-1.5 shrink-0 ${
                    infoTab === 'mechanics'
                      ? 'bg-amber-500 text-slate-950 shadow-md'
                      : 'text-slate-400 hover:text-white hover:bg-slate-800'
                  }`}
                >
                  <Layers className="w-3.5 h-3.5" />
                  <span>Как ловить (2 Шкалы)</span>
                </button>

                <button
                  onClick={() => setInfoTab('chances')}
                  className={`px-3.5 py-1.5 rounded-xl text-xs font-bold transition flex items-center gap-1.5 shrink-0 ${
                    infoTab === 'chances'
                      ? 'bg-amber-500 text-slate-950 shadow-md'
                      : 'text-slate-400 hover:text-white hover:bg-slate-800'
                  }`}
                >
                  <Percent className="w-3.5 h-3.5" />
                  <span>Шансы и Секторы (1-4)</span>
                </button>

                <button
                  onClick={() => setInfoTab('levels')}
                  className={`px-3.5 py-1.5 rounded-xl text-xs font-bold transition flex items-center gap-1.5 shrink-0 ${
                    infoTab === 'levels'
                      ? 'bg-amber-500 text-slate-950 shadow-md'
                      : 'text-slate-400 hover:text-white hover:bg-slate-800'
                  }`}
                >
                  <Target className="w-3.5 h-3.5" />
                  <span>5 Коллекционных уровней</span>
                </button>

                <button
                  onClick={() => setInfoTab('diffs')}
                  className={`px-3.5 py-1.5 rounded-xl text-xs font-bold transition flex items-center gap-1.5 shrink-0 ${
                    infoTab === 'diffs'
                      ? 'bg-amber-500 text-slate-950 shadow-md'
                      : 'text-slate-400 hover:text-white hover:bg-slate-800'
                  }`}
                >
                  <Trophy className="w-3.5 h-3.5" />
                  <span>3 Сложности & Награды</span>
                </button>
              </div>

              {/* Modal Body Content */}
              <div className="p-5 overflow-y-auto flex-1 text-xs space-y-4">
                {/* TAB 1: Mechanics */}
                {infoTab === 'mechanics' && (
                  <div className="space-y-4">
                    {/* Cat Quote */}
                    <div className="bg-amber-950/40 border border-amber-500/40 rounded-2xl p-3.5 flex items-start gap-3">
                      <span className="text-2xl">🐱</span>
                      <div className="space-y-1">
                        <div className="font-bold text-amber-300">Кот-Алхимик объясняет заброс:</div>
                        <p className="text-slate-300 leading-relaxed text-[11px]">
                          «Муррр! Ловля состоит из двух простых шагов. Сначала кликаешь по удочке в правом углу — удочка блокируется, и включается вертикальная шкала. Останови ее в самом верху, в Зоне 4! Затем сразу запустится верхняя горизонтальная шкала: подожди, пока два луча разойдутся до самых краев, и жми подсечку!»
                        </p>
                      </div>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-3.5">
                      {/* Step 1 Card */}
                      <div className="bg-slate-950/80 border border-amber-500/30 rounded-2xl p-4 flex flex-col justify-between gap-3">
                        <div>
                          <div className="flex items-center justify-between mb-2">
                            <span className="text-[10px] font-black uppercase text-amber-400 bg-amber-950 px-2 py-0.5 rounded border border-amber-500/40">
                              Шаг 1: Вертикальная шкала
                            </span>
                            <span className="text-xs text-slate-400">Шкала 1: Дальность</span>
                          </div>
                          <h4 className="text-sm font-bold text-white mb-1">Заброс в верхний Сектор 4</h4>
                          <p className="text-slate-300 leading-relaxed text-[11px]">
                            • Шкала разделена на 4 сектора (3 разделителя).<br />
                            • Бегунок скользит снизу вверх и обратно.<br />
                            • <strong>Сектор 4 (Верхний, красный)</strong> — гарантирует шанс на редчайшие зелья!<br />
                            • <strong>Сектор 1 (Нижний)</strong> — принесет только тину и старую банку.
                          </p>
                        </div>
                        <div className="p-2 bg-slate-900 rounded-xl border border-slate-800 text-[10px] text-amber-300 font-semibold">
                          💡 Совет: На Легком уровне Зона 4 занимает целых 35% шкалы!
                        </div>
                      </div>

                      {/* Step 2 Card */}
                      <div className="bg-slate-950/80 border border-purple-500/30 rounded-2xl p-4 flex flex-col justify-between gap-3">
                        <div>
                          <div className="flex items-center justify-between mb-2">
                            <span className="text-[10px] font-black uppercase text-purple-400 bg-purple-950 px-2 py-0.5 rounded border border-purple-500/40">
                              Шаг 2: Горизонтальная шкала
                            </span>
                            <span className="text-xs text-slate-400">Шкала 2: Подсечка</span>
                          </div>
                          <h4 className="text-sm font-bold text-white mb-1">Сведение лучей к краям</h4>
                          <p className="text-slate-300 leading-relaxed text-[11px]">
                            • Два ярких световых луча стартуют от центра и бегут к внешним краям.<br />
                            • Нажмите кнопку в момент, когда <strong>лучи достигнут максимальной ширины</strong> у краев полоски.<br />
                            • Точная подсечка на краях вместе с Сектором 4 открывает <strong>Драконье Зелье (+3000 XP)</strong>!
                          </p>
                        </div>
                        <div className="p-2 bg-slate-900 rounded-xl border border-slate-800 text-[10px] text-purple-300 font-semibold">
                          ✨ Идеальное попадание: Сектор 4 + Края лучей &gt; 75% точности.
                        </div>
                      </div>
                    </div>
                  </div>
                )}

                {/* TAB 2: Chances and Sectors */}
                {infoTab === 'chances' && (
                  <div className="space-y-4">
                    <div className="bg-slate-950/90 border border-slate-800 rounded-2xl overflow-hidden shadow-lg">
                      <table className="w-full text-left text-[11px] border-collapse">
                        <thead>
                          <tr className="bg-slate-950 text-slate-300 uppercase font-bold text-[10px] border-b border-slate-800">
                            <th className="py-2.5 px-3">Сектор полоски</th>
                            <th className="py-2.5 px-3">Условие попадания</th>
                            <th className="py-2.5 px-3">Что вылавливается</th>
                            <th className="py-2.5 px-3 text-right">Шансы (%)</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-800/60">
                          {/* Sector 1 */}
                          <tr className="hover:bg-slate-800/30">
                            <td className="py-3 px-3 font-bold text-slate-400 align-top">
                              <span className="px-2 py-0.5 rounded bg-slate-800 text-slate-300 border border-slate-700">
                                Сектор 1 (Низ)
                              </span>
                            </td>
                            <td className="py-3 px-3 text-slate-400 align-top">
                              Нижняя часть вертикальной шкалы (&lt;25%)
                            </td>
                            <td className="py-3 px-3 text-slate-300 align-top space-y-0.5">
                              <div>• Старая стеклянная банка (0 XP)</div>
                              <div>• Болотная тина и мокрый камень (0 XP)</div>
                              <div className="text-emerald-400 font-semibold">• Малое Зелье Опыта (+10 XP)</div>
                            </td>
                            <td className="py-3 px-3 text-right font-mono font-bold text-slate-300 align-top space-y-0.5">
                              <div>45%</div>
                              <div>45%</div>
                              <div className="text-emerald-400">10%</div>
                            </td>
                          </tr>

                          {/* Sector 2 & 3 */}
                          <tr className="hover:bg-slate-800/30 bg-slate-950/40">
                            <td className="py-3 px-3 font-bold text-blue-300 align-top">
                              <span className="px-2 py-0.5 rounded bg-blue-950 text-blue-300 border border-blue-500/40">
                                Секторы 2 и 3
                              </span>
                            </td>
                            <td className="py-3 px-3 text-slate-400 align-top">
                              Средняя высота заброса (25% - 78%)
                            </td>
                            <td className="py-3 px-3 text-slate-300 align-top space-y-0.5">
                              <div className="text-cyan-400 font-semibold">• Магический Рунный Камень (+25 XP)</div>
                              <div className="text-emerald-400 font-semibold">• Малое Зелье Опыта (+10 XP)</div>
                              <div className="text-blue-400 font-semibold">• Среднее Зелье (+50 XP)</div>
                              <div className="text-purple-400 font-semibold">• Высокое Зелье (+100 XP)</div>
                            </td>
                            <td className="py-3 px-3 text-right font-mono font-bold text-slate-300 align-top space-y-0.5">
                              <div className="text-cyan-400">25%</div>
                              <div className="text-emerald-400">25%</div>
                              <div className="text-blue-400">15%</div>
                              <div className="text-purple-400">10%</div>
                            </td>
                          </tr>

                          {/* Sector 4 */}
                          <tr className="hover:bg-slate-800/30 bg-amber-950/20">
                            <td className="py-3 px-3 font-bold text-amber-300 align-top">
                              <span className="px-2 py-0.5 rounded bg-amber-950 text-amber-300 border border-amber-500/50">
                                Сектор 4 (Верх)
                              </span>
                            </td>
                            <td className="py-3 px-3 text-amber-200/80 align-top">
                              Верхняя зона + лучи у краев (&gt;75%)
                            </td>
                            <td className="py-3 px-3 text-slate-200 align-top space-y-0.5">
                              <div className="text-purple-400 font-semibold">• Высокое Зелье (+100 XP)</div>
                              <div className="text-pink-400 font-semibold">• Магическое Зелье (+300 XP)</div>
                              <div className="text-amber-400 font-semibold">• Легендарное Зелье (+500 XP)</div>
                              <div className="text-purple-300 font-semibold">• Мифическое Зелье (+1 000 XP)</div>
                              <div className="text-red-400 font-extrabold flex items-center gap-1">
                                <Flame className="w-3 h-3 text-red-500" />
                                <span>• ДРАКОНЬЕ ЗЕЛЬЕ (+3 000 XP)</span>
                              </div>
                            </td>
                            <td className="py-3 px-3 text-right font-mono font-bold text-slate-300 align-top space-y-0.5">
                              <div className="text-purple-400">35%</div>
                              <div className="text-pink-400">30%</div>
                              <div className="text-amber-400">20%</div>
                              <div className="text-purple-300">10%</div>
                              <div className="text-red-400 font-extrabold">5%</div>
                            </td>
                          </tr>
                        </tbody>
                      </table>
                    </div>
                  </div>
                )}

                {/* TAB 3: 5 Collection Levels */}
                {infoTab === 'levels' && (
                  <div className="space-y-3">
                    <div className="text-[11px] text-slate-300 bg-slate-950/80 p-3 rounded-xl border border-slate-800">
                      💡 <strong>5 Уровней коллекционных задач:</strong> Чем легче уровень сложности вы выбираете в игре, тем проще вытащить нужное количество целевых предметов благодаря более широкой Зоне 4!
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                      {FISHING_COLLECTION_LEVELS.map(task => (
                        <div
                          key={task.levelNumber}
                          className="bg-slate-950/80 border border-slate-800 hover:border-amber-500/50 rounded-2xl p-3.5 flex flex-col justify-between gap-2.5 transition"
                        >
                          <div>
                            <div className="flex items-center justify-between mb-1.5">
                              <span className="text-[10px] font-black uppercase text-amber-400 bg-amber-950 px-2 py-0.5 rounded border border-amber-500/40">
                                Уровень {task.levelNumber}
                              </span>
                              <span className="text-xs font-bold text-emerald-400">
                                Цель: x{task.requiredCount} шт
                              </span>
                            </div>
                            <h4 className="text-xs font-bold text-white mb-0.5">{task.title}</h4>
                            <p className="text-[11px] text-slate-400 leading-relaxed mb-2">
                              {task.hint}
                            </p>
                          </div>

                          <div className="space-y-1.5 pt-2 border-t border-slate-800/80 text-[10px]">
                            <div className="text-amber-300 font-semibold">
                              🎁 Награда: {task.rewardDescription}
                            </div>
                            <div className="text-slate-400">
                              <span className="text-emerald-400 font-semibold">Легкий:</span> {task.easyAdvice}
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {/* TAB 4: 3 Difficulties */}
                {infoTab === 'diffs' && (
                  <div className="space-y-4">
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-3.5">
                      {/* Easy */}
                      <div className="bg-slate-950/80 border-2 border-emerald-500/40 rounded-2xl p-4 flex flex-col justify-between gap-3">
                        <div>
                          <div className="text-xs font-black text-emerald-400 uppercase tracking-wider mb-1">
                            Легкий уровень
                          </div>
                          <div className="text-white font-bold text-sm mb-1">Тихий Залив</div>
                          <p className="text-[11px] text-slate-400">
                            10 попыток • Зона 4: 35% (широкая) • Скорость x1.0
                          </p>
                        </div>
                        <div className="space-y-1.5 text-xs bg-slate-900 p-2.5 rounded-xl border border-slate-800">
                          <div className="flex justify-between text-amber-300 font-semibold"><span>Золото:</span><span>+3 000</span></div>
                          <div className="flex justify-between text-blue-300 font-semibold"><span>Камни:</span><span>+3 шт</span></div>
                          <div className="flex justify-between text-purple-300 font-semibold"><span>Свитки:</span><span>+1 шт</span></div>
                        </div>
                      </div>

                      {/* Medium */}
                      <div className="bg-slate-950/80 border-2 border-blue-500/40 rounded-2xl p-4 flex flex-col justify-between gap-3">
                        <div>
                          <div className="text-xs font-black text-blue-400 uppercase tracking-wider mb-1">
                            Средний уровень
                          </div>
                          <div className="text-white font-bold text-sm mb-1">Туманный Пруд</div>
                          <p className="text-[11px] text-slate-400">
                            10 попыток • Зона 4: 22% (средняя) • Скорость x1.4
                          </p>
                        </div>
                        <div className="space-y-1.5 text-xs bg-slate-900 p-2.5 rounded-xl border border-slate-800">
                          <div className="flex justify-between text-amber-300 font-semibold"><span>Золото:</span><span>+5 000</span></div>
                          <div className="flex justify-between text-blue-300 font-semibold"><span>Камни:</span><span>+5 шт</span></div>
                          <div className="flex justify-between text-purple-300 font-semibold"><span>Свитки:</span><span>+2 шт</span></div>
                        </div>
                      </div>

                      {/* Hard */}
                      <div className="bg-slate-950/80 border-2 border-amber-500/50 rounded-2xl p-4 flex flex-col justify-between gap-3">
                        <div>
                          <div className="text-xs font-black text-amber-400 uppercase tracking-wider mb-1">
                            Сложный уровень
                          </div>
                          <div className="text-white font-bold text-sm mb-1">Омут Дракона</div>
                          <p className="text-[11px] text-slate-400">
                            10 попыток • Зона 4: 12% (узкая) • Скорость x1.9
                          </p>
                        </div>
                        <div className="space-y-1.5 text-xs bg-slate-900 p-2.5 rounded-xl border border-slate-800">
                          <div className="flex justify-between text-amber-300 font-semibold"><span>Золото:</span><span>+10 000</span></div>
                          <div className="flex justify-between text-blue-300 font-semibold"><span>Камни:</span><span>+10 шт</span></div>
                          <div className="flex justify-between text-purple-300 font-semibold"><span>Свитки:</span><span>+5 шт</span></div>
                          <div className="flex justify-between text-amber-400 font-bold bg-amber-950/60 px-2 py-0.5 rounded border border-amber-500/40 text-[11px]">
                            <span>Зелье Мастерства:</span><span>+100 XP</span>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                )}
              </div>

              {/* Modal Footer */}
              <div className="p-3.5 bg-slate-950 border-t border-slate-800 flex items-center justify-between">
                <span className="text-[11px] text-slate-400">
                  Все выловленные зелья автоматически стакаются в сундуке (xN)
                </span>
                <button
                  onClick={() => setShowInfoModal(false)}
                  className="px-5 py-2 bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-400 hover:to-amber-500 text-slate-950 font-black text-xs rounded-xl shadow transition"
                >
                  ПОНЯТНО, К РЫБАЛКЕ!
                </button>
              </div>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
};

