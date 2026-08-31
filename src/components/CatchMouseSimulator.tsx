import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'motion/react';
import { Play, RotateCcw, X, Award, CheckCircle2, Shield, Zap, Sparkles } from 'lucide-react';

interface MouseObj {
  id: number;
  x: number; // percentage across track
  speed: number;
  caught: boolean;
}

export const CatchMouseSimulator: React.FC<{ onOpenFishing?: () => void }> = ({ onOpenFishing }) => {
  const [difficulty, setDifficulty] = useState<'easy' | 'medium' | 'hard'>('easy');
  const [isPlaying, setIsPlaying] = useState<boolean>(false);
  const [mice, setMice] = useState<MouseObj[]>([]);
  const [score, setScore] = useState<number>(0);
  const [targetScore, setTargetScore] = useState<number>(5);
  const [hasWon, setHasWon] = useState<boolean>(false);
  const [catDialogueOpen, setCatDialogueOpen] = useState<boolean>(false);

  // Difficulty parameters
  const diffConfigs = {
    easy: { target: 5, speed: 1.2, name: 'Легкий', xp: '+20 XP', gold: '+1000 G', stones: '+5 Stones', scrolls: '-' },
    medium: { target: 8, speed: 1.8, name: 'Средний', xp: '+50 XP', gold: '+2000 G', stones: '+10 Stones', scrolls: '+1 Scroll' },
    hard: { target: 12, speed: 2.5, name: 'Сложный', xp: '+100 XP', gold: 'Зелье Опыта (+100)', stones: '+20 Stones', scrolls: '+3 Scrolls' },
  };

  const startLevel = (lvl: 'easy' | 'medium' | 'hard') => {
    setDifficulty(lvl);
    setTargetScore(diffConfigs[lvl].target);
    setScore(0);
    setHasWon(false);
    setCatDialogueOpen(false);
    setIsPlaying(true);

    // Spawn initial mice
    const initialMice: MouseObj[] = Array.from({ length: diffConfigs[lvl].target }, (_, i) => ({
      id: i,
      x: -10 - i * 22,
      speed: diffConfigs[lvl].speed * (0.9 + Math.random() * 0.3),
      caught: false,
    }));
    setMice(initialMice);
  };

  // Game loop for moving mice along the road track
  useEffect(() => {
    if (!isPlaying || hasWon) return;

    const interval = setInterval(() => {
      setMice(prev =>
        prev.map(m => {
          if (m.caught) return m;
          let nextX = m.x + m.speed;
          if (nextX > 115) nextX = -15; // wrap around
          return { ...m, x: nextX };
        })
      );
    }, 50);

    return () => clearInterval(interval);
  }, [isPlaying, hasWon]);

  const catchMouse = (id: number) => {
    setMice(prev =>
      prev.map(m => {
        if (m.id === id && !m.caught) {
          const nextScore = score + 1;
          setScore(nextScore);
          if (nextScore >= targetScore) {
            setHasWon(true);
            setIsPlaying(false);
            setTimeout(() => setCatDialogueOpen(true), 600);
          }
          return { ...m, caught: true };
        }
        return m;
      })
    );
  };

  const handleClose = () => {
    setIsPlaying(false);
    setHasWon(false);
    setCatDialogueOpen(false);
  };

  return (
    <div className="flex flex-col gap-6 max-w-5xl mx-auto">
      {/* Header Panel */}
      <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-6 flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <span className="w-2.5 h-2.5 rounded-full bg-amber-400" />
            <h2 className="text-xl font-bold text-white">Мини-игра: Поймай Мышь (CatchMouse_Game_Panel)</h2>
          </div>
          <p className="text-xs text-slate-400">
            Holes_Container (Y=60, W=1000, H=150) • Road_Track (Y=-18, W=960, H=120, RotX=-126.083°)
          </p>
        </div>

        {/* Difficulty Selection */}
        <div className="flex items-center gap-2 bg-slate-950 p-1.5 rounded-xl border border-slate-800">
          {(['easy', 'medium', 'hard'] as const).map(lvl => (
            <button
              key={lvl}
              onClick={() => startLevel(lvl)}
              className={`px-3.5 py-1.5 rounded-lg text-xs font-bold transition ${
                difficulty === lvl && isPlaying
                  ? 'bg-amber-500 text-slate-950 shadow-md'
                  : 'text-slate-400 hover:text-white hover:bg-slate-800'
              }`}
            >
              {diffConfigs[lvl].name}
            </button>
          ))}
        </div>
      </div>

      {/* Main Track Simulator */}
      <div className="relative w-full h-[400px] bg-slate-950 rounded-2xl border border-slate-800 overflow-hidden shadow-2xl p-4 flex flex-col justify-between select-none">
        {/* Top Close Button (Close_Button) */}
        <div className="w-full flex items-center justify-between z-20">
          <div className="flex items-center gap-3 bg-slate-900/90 px-3.5 py-1.5 rounded-xl border border-slate-800">
            <span className="text-xs font-bold text-amber-400">
              Поймано: {score} / {targetScore}
            </span>
            <span className="text-xs text-slate-400">| Уровень: {diffConfigs[difficulty].name}</span>
          </div>

          <button
            onClick={handleClose}
            className="w-8 h-8 rounded-xl bg-slate-900 border border-slate-700 hover:border-red-500 hover:bg-red-950/40 text-slate-300 hover:text-red-400 flex items-center justify-center transition shadow"
            title="Закрыть (HandleCloseButtonClicked)"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Holes Container (Holes_Container: Pos Y = 60, Width = 1000, Height = 150) */}
        <div className="absolute top-[20%] left-1/2 -translate-x-1/2 w-[90%] max-w-[1000px] h-[90px] bg-amber-950/20 border border-amber-900/30 rounded-2xl flex items-center justify-around px-8 z-10">
          {[1, 2, 3, 4, 5, 6].map(hole => (
            <div
              key={hole}
              className="w-14 h-9 bg-slate-950 rounded-full border-2 border-amber-950 shadow-inner flex items-center justify-center relative overflow-hidden"
            >
              <div className="w-10 h-6 bg-black/90 rounded-full blur-[1px]" />
              <span className="absolute text-[9px] text-amber-900/60 font-bold">Нора {hole}</span>
            </div>
          ))}
        </div>

        {/* Road Track (Road_Track: Pos Y = -18, Width = 960, Height = 120, Rotation X = -126.083) */}
        <div 
          className="absolute bottom-[22%] left-1/2 -translate-x-1/2 w-[90%] max-w-[960px] h-[100px] rounded-2xl border-2 border-stone-700 shadow-2xl z-10 overflow-hidden flex items-center"
          style={{
            background: 'linear-gradient(180deg, #292524 0%, #1c1917 100%)',
            boxShadow: '0 10px 30px rgba(0,0,0,0.8), inset 0 2px 4px rgba(255,255,255,0.1)',
          }}
        >
          {/* Cobblestone Pattern */}
          <div className="absolute inset-0 opacity-25 bg-[radial-gradient(circle_at_50%_50%,#a8a29e_2px,transparent_2px)] bg-[length:16px_16px]" />
          <div className="absolute top-0 left-0 right-0 h-2 bg-stone-600/40 border-b border-stone-800" />
          <div className="absolute bottom-0 left-0 right-0 h-2 bg-stone-900 border-t border-stone-800" />

          {/* Mice running on edge */}
          {isPlaying &&
            mice.map(m => {
              if (m.caught) return null;
              return (
                <div
                  key={m.id}
                  onClick={() => catchMouse(m.id)}
                  className="absolute cursor-pointer hover:scale-125 transition-transform text-2xl flex items-center justify-center drop-shadow-[0_4px_8px_rgba(0,0,0,0.9)]"
                  style={{
                    left: `${m.x}%`,
                    top: '28%',
                  }}
                  title="Кликните чтобы поймать мышь!"
                >
                  🐭
                </div>
              );
            })}
        </div>

        {/* Start Overlay if not playing */}
        {!isPlaying && !hasWon && !catDialogueOpen && (
          <div className="absolute inset-0 flex flex-col items-center justify-center bg-slate-950/80 backdrop-blur-sm z-20">
            <h3 className="text-lg font-bold text-white mb-1">Готовы к охоте на мышей?</h3>
            <p className="text-xs text-slate-300 mb-4 max-w-sm text-center">
              Мышки бегают по кромке каменной дороги Road_Track. Кликайте по ним, чтобы поймать!
            </p>
            <button
              onClick={() => startLevel(difficulty)}
              className="px-6 py-3 bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-400 hover:to-amber-500 text-slate-950 text-xs font-extrabold rounded-xl shadow-xl transition transform hover:scale-105 flex items-center gap-2"
            >
              <Play className="w-4 h-4 text-slate-950 fill-current" />
              <span>НАЧАТЬ ОХОТУ ({diffConfigs[difficulty].name})</span>
            </button>
          </div>
        )}

        {/* Cat Victory & Consent Dialogue Modal */}
        <AnimatePresence>
          {catDialogueOpen && (
            <motion.div
              initial={{ opacity: 0, scale: 0.9 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0 }}
              className="absolute inset-4 bg-slate-950/95 backdrop-blur-xl border-2 border-amber-500/70 rounded-2xl p-6 shadow-2xl z-30 flex flex-col items-center justify-center text-center"
            >
              <div className="w-14 h-14 rounded-full bg-amber-500/20 border border-amber-400/50 flex items-center justify-center text-3xl mb-2">
                🐱
              </div>

              <span className="text-xs font-bold text-emerald-400 uppercase tracking-widest mb-1">
                ПОБЕДА! ВСЕ МЫШИ ПОЙМАНЫ!
              </span>

              <h4 className="text-base font-bold text-white mb-2">
                Диалог Кота-Алхимика
              </h4>

              <div className="bg-slate-900/90 border border-slate-800 p-3.5 rounded-xl max-w-md text-xs text-slate-300 leading-relaxed italic mb-4">
                «Ты смог меня удивить, что поймал всех мышей! За это я дарю тебе подарок: {diffConfigs[difficulty].xp}, {diffConfigs[difficulty].gold}, {diffConfigs[difficulty].stones}! Но Колесо Игр заблокировано, ведь у меня есть секрет...»
              </div>

              <div className="flex items-center gap-3">
                <button
                  onClick={() => {
                    setCatDialogueOpen(false);
                    if (onOpenFishing) onOpenFishing();
                  }}
                  className="px-6 py-2.5 bg-gradient-to-r from-teal-500 to-emerald-500 hover:from-teal-400 hover:to-emerald-400 text-slate-950 font-bold text-xs rounded-xl shadow-lg transition transform hover:scale-105 flex items-center gap-2"
                >
                  <Sparkles className="w-4 h-4" />
                  <span>СОГЛАСЕН! (ПЕРЕЙТИ К РЫБАЛКЕ)</span>
                </button>

                <button
                  onClick={() => {
                    setCatDialogueOpen(false);
                    startLevel(difficulty);
                  }}
                  className="px-4 py-2.5 bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold rounded-xl transition"
                >
                  Сыграть еще раз
                </button>
              </div>
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </div>
  );
};
