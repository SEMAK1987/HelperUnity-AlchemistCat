import React, { useState } from 'react';
import { MessageSquare, Check, Sparkles, Trophy, Lock, Play, ChevronRight, ShieldAlert, Percent, Target, Layers, HelpCircle } from 'lucide-react';
import { CAT_STORY_STEPS } from '../data/dialoguesData';
import { FISHING_COLLECTION_LEVELS } from '../data/fishingData';

export const CatDialogueViewer: React.FC = () => {
  const [activeStep, setActiveStep] = useState<number>(1);
  const [agreedToFishing, setAgreedToFishing] = useState<boolean>(false);

  const activeStepData = CAT_STORY_STEPS.find(s => s.step === activeStep) || CAT_STORY_STEPS[0];

  return (
    <div className="flex flex-col gap-6 max-w-5xl mx-auto">
      {/* Header */}
      <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-6">
        <div className="flex items-center gap-3 mb-2">
          <div className="w-10 h-10 rounded-xl bg-amber-500/20 border border-amber-500/40 flex items-center justify-center text-amber-400">
            <MessageSquare className="w-5 h-5" />
          </div>
          <div>
            <h2 className="text-xl font-bold text-white">ЧАСТЬ 5. Диалоги Кота-Алхимика и сценарий обучения</h2>
            <p className="text-xs text-slate-400">
              Пошаговый сюжет: от выбора сложности до шансов дропа, 5 уровней сбора и квеста поиска вещей
            </p>
          </div>
        </div>

        {/* Step Progress Indicators */}
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-2 mt-4">
          {CAT_STORY_STEPS.map(s => (
            <button
              key={s.step}
              onClick={() => setActiveStep(s.step)}
              className={`p-2.5 rounded-xl border text-left transition flex flex-col justify-between ${
                activeStep === s.step
                  ? 'bg-amber-950/60 border-amber-500/80 text-amber-200 shadow-lg'
                  : 'bg-slate-950 border-slate-800 text-slate-400 hover:text-slate-200 hover:border-slate-700'
              }`}
            >
              <div className="flex items-center justify-between w-full mb-1">
                <span className="text-[10px] font-bold uppercase tracking-wider text-amber-400">
                  Шаг {s.step}
                </span>
              </div>
              <span className="text-[11px] font-semibold line-clamp-2">{s.title}</span>
            </button>
          ))}
        </div>
      </div>

      {/* Active Step Visualizer */}
      <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-6 flex flex-col gap-5 shadow-xl">
        <div className="flex items-center gap-3">
          <div className="w-12 h-12 rounded-2xl bg-amber-500/20 border border-amber-500/50 flex items-center justify-center text-3xl shadow">
            🐱
          </div>
          <div>
            <h3 className="text-base font-bold text-white">{activeStepData.title}</h3>
            <p className="text-xs text-emerald-400">{activeStepData.speaker} • {activeStepData.tag}</p>
          </div>
        </div>

        {/* Speech Bubble */}
        <div className="p-4 bg-slate-950 rounded-2xl border border-slate-800 text-xs text-slate-200 leading-relaxed whitespace-pre-line">
          {activeStepData.content}
        </div>

        {/* Step-specific rich panels */}
        {activeStep === 1 && (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3 pt-2">
            {activeStepData.levels?.map((lvl, idx) => (
              <div key={idx} className="p-4 rounded-xl bg-slate-950 border border-slate-800 flex flex-col gap-2">
                <div className="text-xs font-bold text-amber-300 border-b border-slate-800 pb-1.5 flex items-center justify-between">
                  <span>{lvl.name}</span>
                </div>
                <div className="text-[11px] text-slate-400">{lvl.xp}</div>
                <div className="text-xs text-slate-300 space-y-1 mt-1">
                  <div>💰 {lvl.gold}</div>
                  <div>💎 {lvl.stones}</div>
                  <div>📜 {lvl.scrolls}</div>
                  {lvl.potions !== '-' && (
                    <div className="text-amber-400 font-bold bg-amber-950/40 px-2 py-0.5 rounded border border-amber-500/30 text-[10px]">
                      🎁 {lvl.potions}
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}

        {activeStep === 2 && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3.5 pt-2">
            <div className="p-4 rounded-xl bg-slate-950 border border-amber-500/30 text-xs space-y-1.5">
              <div className="font-bold text-amber-300 flex items-center gap-1.5">
                <Layers className="w-4 h-4 text-amber-400" />
                <span>1. Вертикальная шкала (Шкала 1)</span>
              </div>
              <p className="text-slate-300 text-[11px] leading-relaxed">
                4 сектора с 3 разделителями. Бегунок двигается вертикально. Задача игрока — зафиксировать его в верхнем Секторе 4 для дальнего заброса в глубину пруда!
              </p>
            </div>

            <div className="p-4 rounded-xl bg-slate-950 border border-purple-500/30 text-xs space-y-1.5">
              <div className="font-bold text-purple-300 flex items-center gap-1.5">
                <Sparkles className="w-4 h-4 text-purple-400" />
                <span>2. Горизонтальная шкала (Шкала 2)</span>
              </div>
              <p className="text-slate-300 text-[11px] leading-relaxed">
                Два световых луча синхронно расходятся от центра полосы к ее краям. Задача — подсечь ровно в момент, когда лучи дойдут до внешних краев!
              </p>
            </div>
          </div>
        )}

        {activeStep === 3 && (
          <div className="p-4 rounded-xl bg-slate-950 border border-slate-800 text-xs space-y-2">
            <div className="font-bold text-amber-300 flex items-center gap-1.5">
              <Percent className="w-4 h-4 text-amber-400" />
              <span>Шансы и Секторы:</span>
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-2.5 text-[11px]">
              <div className="p-2.5 rounded-lg bg-slate-900 border border-slate-800">
                <div className="font-bold text-slate-400">Сектор 1 (Низ)</div>
                <div className="text-slate-300">Тина (45%), Банка (45%), Малое зелье (10%)</div>
              </div>
              <div className="p-2.5 rounded-lg bg-slate-900 border border-slate-800">
                <div className="font-bold text-blue-400">Секторы 2-3 (Середина)</div>
                <div className="text-slate-300">Руны (25%), Среднее (15%), Высокое (10%)</div>
              </div>
              <div className="p-2.5 rounded-lg bg-slate-900 border border-amber-500/40">
                <div className="font-bold text-amber-400">Сектор 4 (Верх + Края)</div>
                <div className="text-slate-200 font-semibold">Драконье (5%), Мифическое (10%), Легендарное (20%)</div>
              </div>
            </div>
          </div>
        )}

        {activeStep === 4 && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2.5 pt-2">
            {FISHING_COLLECTION_LEVELS.map(t => (
              <div key={t.levelNumber} className="p-3 rounded-xl bg-slate-950 border border-slate-800 text-xs space-y-1">
                <div className="flex justify-between font-bold text-amber-300 text-[11px]">
                  <span>{t.title}</span>
                  <span className="text-emerald-400">x{t.requiredCount} шт</span>
                </div>
                <p className="text-slate-400 text-[10px]">{t.hint}</p>
                <div className="text-emerald-300 text-[10px]">Награда: {t.rewardDescription}</div>
              </div>
            ))}
          </div>
        )}

        {activeStep === 5 && (
          <div className="p-4 rounded-xl bg-slate-950 border border-slate-800 flex items-center justify-between">
            <div className="space-y-1 text-xs">
              <div className="font-bold text-amber-300">📦 Автоматический стак в сундуке</div>
              <p className="text-slate-400 text-[11px]">
                Одинаковые предметы объединяются в один слот со значком xN и суммарным опытом.
              </p>
            </div>
            <div className="px-3 py-1 bg-amber-500/20 border border-amber-500/40 rounded-lg text-amber-300 font-bold text-xs">
              x1 ... x10 в одном слоте
            </div>
          </div>
        )}

        {activeStep === 6 && (
          <div className="p-4 rounded-xl bg-slate-950 border border-teal-500/40 flex flex-col gap-3">
            <div className="text-xs font-bold text-teal-300">
              🗺️ Новая игра: «Поиск предметов в 3 локациях»:
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-2 text-xs text-slate-300">
              <div className="p-2.5 rounded-lg bg-slate-900 border border-slate-800">
                1. Алхимическая Лавка 🧪
              </div>
              <div className="p-2.5 rounded-lg bg-slate-900 border border-slate-800">
                2. Старый заброшенный Дом 🏚️
              </div>
              <div className="p-2.5 rounded-lg bg-slate-900 border border-slate-800">
                3. Рынок старинных вещей 🏺
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

