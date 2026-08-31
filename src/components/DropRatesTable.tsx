import React, { useState } from 'react';
import { Table, Flame, Sparkles, Percent, Award, ChevronRight } from 'lucide-react';
import { DROP_CONDITIONS, LOOT_ITEMS } from '../data/fishingData';

export const DropRatesTable: React.FC = () => {
  const [testVertical, setTestVertical] = useState<number>(0.85);
  const [testHorizontal, setTestHorizontal] = useState<number>(0.5);

  // Calculate live score
  const centerAccuracy = 1 - Math.abs(testHorizontal - 0.5) * 2;
  const score = testVertical * 0.55 + centerAccuracy * 0.45;

  let currentCategory = '3. Идеальный / Дальний';
  let categoryColor = 'text-emerald-400';
  let bgBadge = 'bg-emerald-950/70 border-emerald-500/50';

  if (score < 0.45) {
    currentCategory = '1. Плохой / Близкий';
    categoryColor = 'text-slate-400';
    bgBadge = 'bg-slate-900 border-slate-700';
  } else if (score < 0.8) {
    currentCategory = '2. Средний';
    categoryColor = 'text-blue-400';
    bgBadge = 'bg-blue-950/70 border-blue-500/50';
  }

  return (
    <div className="flex flex-col gap-6 max-w-5xl mx-auto">
      {/* Header */}
      <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-6">
        <div className="flex items-center gap-3 mb-2">
          <div className="w-10 h-10 rounded-xl bg-amber-500/20 border border-amber-500/40 flex items-center justify-center text-amber-400">
            <Percent className="w-5 h-5" />
          </div>
          <div>
            <h2 className="text-xl font-bold text-white">ЧАСТЬ 3. Таблица шансов дропа и расчет улова</h2>
            <p className="text-xs text-slate-400">
              Матрица вероятностей наград в зависимости от силы заброса и точности подсечки
            </p>
          </div>
        </div>

        {/* Live Interactive Quality Calculator */}
        <div className="mt-5 p-4 rounded-xl bg-slate-950 border border-slate-800 flex flex-col md:flex-row items-center justify-between gap-6">
          <div className="flex-1 w-full flex flex-col gap-3">
            <div>
              <div className="flex justify-between text-xs font-semibold mb-1 text-slate-300">
                <span>Вертикальная шкала заброса (Дальность):</span>
                <span className="text-amber-400 font-bold">{Math.round(testVertical * 100)}%</span>
              </div>
              <input
                type="range"
                min="0"
                max="1"
                step="0.01"
                value={testVertical}
                onChange={e => setTestVertical(parseFloat(e.target.value))}
                className="w-full accent-amber-500 cursor-pointer"
              />
            </div>

            <div>
              <div className="flex justify-between text-xs font-semibold mb-1 text-slate-300">
                <span>Горизонтальная шкала (Центрирование):</span>
                <span className="text-purple-400 font-bold">
                  {Math.round(centerAccuracy * 100)}% точность к центру
                </span>
              </div>
              <input
                type="range"
                min="0"
                max="1"
                step="0.01"
                value={testHorizontal}
                onChange={e => setTestHorizontal(parseFloat(e.target.value))}
                className="w-full accent-purple-500 cursor-pointer"
              />
            </div>
          </div>

          <div className="flex flex-col items-center md:items-end justify-center min-w-[220px]">
            <span className="text-[11px] uppercase tracking-wider text-slate-400 font-bold mb-1">
              Результирующая категория:
            </span>
            <div className={`px-4 py-2 rounded-xl border text-sm font-extrabold ${bgBadge} ${categoryColor}`}>
              {currentCategory} ({Math.round(score * 100)}/100)
            </div>
            {score >= 0.8 && (
              <span className="text-[11px] text-amber-300 font-bold mt-1.5 flex items-center gap-1">
                <Flame className="w-3.5 h-3.5 text-red-500" />
                Возможен дроп Драконьего Зелья (5%)!
              </span>
            )}
          </div>
        </div>
      </div>

      {/* Guaranteed Difficulty Rewards Matrix */}
      <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-6 shadow-xl">
        <div className="flex items-center gap-3 mb-4">
          <div className="w-10 h-10 rounded-xl bg-purple-500/20 border border-purple-500/40 flex items-center justify-center text-purple-400">
            <Award className="w-5 h-5" />
          </div>
          <div>
            <h3 className="text-lg font-bold text-white">Гарантированные награды за 3 уровня сложности (10 попыток)</h3>
            <p className="text-xs text-slate-400">
              Базовый призовой фонд начисляется в инвентарь игрока вместе со всеми 10 выловленными зельями
            </p>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="bg-slate-950/80 border border-emerald-500/30 rounded-xl p-4 flex flex-col justify-between gap-3">
            <div>
              <div className="text-xs font-black text-emerald-400 uppercase tracking-wider mb-1">Легкий уровень</div>
              <div className="text-xs text-slate-300">10 попыток • Зона 4: 35% (широкая) • Скорость x1.0</div>
            </div>
            <div className="space-y-1.5 text-xs">
              <div className="flex justify-between text-amber-300 font-semibold"><span>Золото:</span><span>+3 000</span></div>
              <div className="flex justify-between text-blue-300 font-semibold"><span>Камни:</span><span>+3 шт</span></div>
              <div className="flex justify-between text-purple-300 font-semibold"><span>Свитки:</span><span>+1 шт</span></div>
            </div>
          </div>

          <div className="bg-slate-950/80 border border-blue-500/40 rounded-xl p-4 flex flex-col justify-between gap-3">
            <div>
              <div className="text-xs font-black text-blue-400 uppercase tracking-wider mb-1">Средний уровень</div>
              <div className="text-xs text-slate-300">10 попыток • Зона 4: 22% (средняя) • Скорость x1.4</div>
            </div>
            <div className="space-y-1.5 text-xs">
              <div className="flex justify-between text-amber-300 font-semibold"><span>Золото:</span><span>+5 000</span></div>
              <div className="flex justify-between text-blue-300 font-semibold"><span>Камни:</span><span>+5 шт</span></div>
              <div className="flex justify-between text-purple-300 font-semibold"><span>Свитки:</span><span>+2 шт</span></div>
            </div>
          </div>

          <div className="bg-slate-950/80 border border-amber-500/50 rounded-xl p-4 flex flex-col justify-between gap-3">
            <div>
              <div className="text-xs font-black text-amber-400 uppercase tracking-wider mb-1">Сложный уровень</div>
              <div className="text-xs text-slate-300">10 попыток • Зона 4: 12% (узкая) • Скорость x1.9</div>
            </div>
            <div className="space-y-1.5 text-xs">
              <div className="flex justify-between text-amber-300 font-semibold"><span>Золото:</span><span>+10 000</span></div>
              <div className="flex justify-between text-blue-300 font-semibold"><span>Камни:</span><span>+10 шт</span></div>
              <div className="flex justify-between text-purple-300 font-semibold"><span>Свитки:</span><span>+5 шт</span></div>
              <div className="flex justify-between text-amber-400 font-bold bg-amber-950/50 px-2 py-0.5 rounded border border-amber-500/30">
                <span>Зелье Мастерства:</span><span>+100 XP</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Main Table */}
      <div className="bg-slate-900/80 border border-slate-800 rounded-2xl overflow-hidden shadow-xl">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs border-collapse">
            <thead>
              <tr className="bg-slate-950/90 text-slate-300 uppercase font-bold text-[11px] border-b border-slate-800">
                <th className="py-3.5 px-4">Качество заброса и ловли</th>
                <th className="py-3.5 px-4">Условие шкал</th>
                <th className="py-3.5 px-4">Возможный улов</th>
                <th className="py-3.5 px-4 text-right">Шансы (%)</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/60">
              {/* Row 1 */}
              <tr className="hover:bg-slate-800/40 transition">
                <td className="py-4 px-4 font-bold text-slate-300 align-top">
                  <div className="flex items-center gap-2">
                    <span className="w-2.5 h-2.5 rounded-full bg-slate-500" />
                    <span>1. Плохой / Близкий</span>
                  </div>
                </td>
                <td className="py-4 px-4 text-slate-400 align-top">
                  Попал в нижнюю зону шкалы заброса (Рейтинг &lt; 0.45)
                </td>
                <td className="py-4 px-4 text-slate-300 align-top space-y-1">
                  <div>• Пустая бутылка / осколки стекла</div>
                  <div>• Тина и мокрый камень</div>
                  <div className="text-emerald-400 font-semibold">• Малое зелье (+10 XP)</div>
                </td>
                <td className="py-4 px-4 text-right font-mono font-bold text-slate-300 align-top space-y-1">
                  <div>45%</div>
                  <div>45%</div>
                  <div className="text-emerald-400">10%</div>
                </td>
              </tr>

              {/* Row 2 */}
              <tr className="hover:bg-slate-800/40 transition bg-slate-950/30">
                <td className="py-4 px-4 font-bold text-blue-300 align-top">
                  <div className="flex items-center gap-2">
                    <span className="w-2.5 h-2.5 rounded-full bg-blue-500" />
                    <span>2. Средний</span>
                  </div>
                </td>
                <td className="py-4 px-4 text-slate-400 align-top">
                  Средняя дальность + попадание в среднюю зону
                </td>
                <td className="py-4 px-4 text-slate-300 align-top space-y-1">
                  <div className="text-cyan-400 font-semibold">• Магический Рунный камень</div>
                  <div>• Тина и речной камень</div>
                  <div className="text-emerald-400 font-semibold">• Малое зелье (+10 XP)</div>
                  <div className="text-blue-400 font-semibold">• Среднее зелье (+50 XP)</div>
                  <div className="text-purple-400 font-semibold">• Высокое зелье (+100 XP)</div>
                </td>
                <td className="py-4 px-4 text-right font-mono font-bold text-slate-300 align-top space-y-1">
                  <div className="text-cyan-400">25%</div>
                  <div>25%</div>
                  <div className="text-emerald-400">25%</div>
                  <div className="text-blue-400">15%</div>
                  <div className="text-purple-400">10%</div>
                </td>
              </tr>

              {/* Row 3 */}
              <tr className="hover:bg-slate-800/40 transition bg-amber-950/20">
                <td className="py-4 px-4 font-bold text-amber-300 align-top">
                  <div className="flex items-center gap-2">
                    <span className="w-2.5 h-2.5 rounded-full bg-amber-400 shadow-[0_0_8px_#f59e0b]" />
                    <span>3. Идеальный / Дальний</span>
                  </div>
                </td>
                <td className="py-4 px-4 text-amber-200/80 align-top">
                  Верхняя зона заброса + Центр горизонтальной шкалы
                </td>
                <td className="py-4 px-4 text-slate-200 align-top space-y-1">
                  <div className="text-purple-400 font-semibold">• Высокое зелье (+100 XP)</div>
                  <div className="text-pink-400 font-semibold">• Магическое зелье (+300 XP)</div>
                  <div className="text-amber-400 font-semibold">• Легендарное зелье (+500 XP)</div>
                  <div className="text-purple-300 font-semibold">• Мифическое зелье (+1000 XP)</div>
                  <div className="text-red-400 font-extrabold flex items-center gap-1">
                    <Flame className="w-3.5 h-3.5 text-red-500" />
                    <span>• ДРАКОНЬЕ ЗЕЛЬЕ (+3000 XP)</span>
                  </div>
                </td>
                <td className="py-4 px-4 text-right font-mono font-bold text-slate-300 align-top space-y-1">
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
    </div>
  );
};
