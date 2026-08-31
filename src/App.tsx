import React, { useState } from 'react';
import { 
  Fish, 
  MousePointer, 
  Layers, 
  Percent, 
  Code, 
  MessageSquare, 
  Image as ImageIcon,
  Sparkles,
  Flame,
  ShieldCheck
} from 'lucide-react';
import { ActiveTab } from './types';
import { FishingSimulator } from './components/FishingSimulator';
import { CatchMouseSimulator } from './components/CatchMouseSimulator';
import { UnityHierarchyGuide } from './components/UnityHierarchyGuide';
import { DropRatesTable } from './components/DropRatesTable';
import { ScriptsViewer } from './components/ScriptsViewer';
import { CatDialogueViewer } from './components/CatDialogueViewer';
import { AssetGallery } from './components/AssetGallery';

export const App: React.FC = () => {
  const [activeTab, setActiveTab] = useState<ActiveTab>('fishing_sim');

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col font-sans selection:bg-amber-500 selection:text-slate-950">
      {/* Top Main Navigation Header */}
      <header className="sticky top-0 z-50 bg-slate-950/85 backdrop-blur-xl border-b border-slate-800 shadow-md">
        <div className="max-w-7xl mx-auto px-4 py-3 flex flex-col md:flex-row items-center justify-between gap-3">
          {/* Logo / Brand Title */}
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-amber-400 via-amber-500 to-amber-600 flex items-center justify-center text-2xl shadow-lg border border-amber-300">
              🐱
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h1 className="text-base font-extrabold text-white tracking-wide">
                  Кот-Алхимик: Студия &amp; Игровой Ассистент
                </h1>
                <span className="text-[10px] px-2 py-0.5 rounded-full bg-amber-500/20 border border-amber-500/40 text-amber-300 font-bold">
                  v18.12.45
                </span>
              </div>
              <p className="text-xs text-slate-400">
                Алхимическая Рыбалка • 2 Шкалы • Ловля Мышей • Unity Hierarchy &amp; C#
              </p>
            </div>
          </div>

          {/* Quick Action Badges */}
          <div className="flex items-center gap-2">
            <div className="flex items-center gap-1.5 px-3 py-1 bg-emerald-950/60 border border-emerald-500/40 rounded-xl text-xs text-emerald-300 font-semibold shadow">
              <ShieldCheck className="w-3.5 h-3.5 text-emerald-400" />
              <span>Unity C# Готов</span>
            </div>
            <div className="flex items-center gap-1.5 px-3 py-1 bg-red-950/60 border border-red-500/40 rounded-xl text-xs text-red-300 font-semibold shadow">
              <Flame className="w-3.5 h-3.5 text-red-400" />
              <span>Драконье Зелье +3000 XP</span>
            </div>
          </div>
        </div>

        {/* Tab Navigation Menu */}
        <div className="max-w-7xl mx-auto px-4 overflow-x-auto no-scrollbar flex items-center gap-1.5 py-1.5 border-t border-slate-900">
          <button
            onClick={() => setActiveTab('fishing_sim')}
            className={`px-3.5 py-2 rounded-xl text-xs font-bold transition flex items-center gap-2 shrink-0 ${
              activeTab === 'fishing_sim'
                ? 'bg-gradient-to-r from-emerald-600 to-teal-600 text-white shadow-md'
                : 'text-slate-400 hover:text-white hover:bg-slate-900'
            }`}
          >
            <Fish className="w-4 h-4 text-emerald-400" />
            <span>Симулятор Рыбалки (2 Шкалы)</span>
          </button>

          <button
            onClick={() => setActiveTab('mouse_sim')}
            className={`px-3.5 py-2 rounded-xl text-xs font-bold transition flex items-center gap-2 shrink-0 ${
              activeTab === 'mouse_sim'
                ? 'bg-amber-500 text-slate-950 shadow-md'
                : 'text-slate-400 hover:text-white hover:bg-slate-900'
            }`}
          >
            <MousePointer className="w-4 h-4" />
            <span>Поймай Мышь (Road_Track)</span>
          </button>

          <button
            onClick={() => setActiveTab('hierarchy_guide')}
            className={`px-3.5 py-2 rounded-xl text-xs font-bold transition flex items-center gap-2 shrink-0 ${
              activeTab === 'hierarchy_guide'
                ? 'bg-purple-600 text-white shadow-md'
                : 'text-slate-400 hover:text-white hover:bg-slate-900'
            }`}
          >
            <Layers className="w-4 h-4" />
            <span>ЧАСТЬ 2. Unity Hierarchy</span>
          </button>

          <button
            onClick={() => setActiveTab('drop_table')}
            className={`px-3.5 py-2 rounded-xl text-xs font-bold transition flex items-center gap-2 shrink-0 ${
              activeTab === 'drop_table'
                ? 'bg-blue-600 text-white shadow-md'
                : 'text-slate-400 hover:text-white hover:bg-slate-900'
            }`}
          >
            <Percent className="w-4 h-4" />
            <span>ЧАСТЬ 3. Шансы Дропа</span>
          </button>

          <button
            onClick={() => setActiveTab('scripts')}
            className={`px-3.5 py-2 rounded-xl text-xs font-bold transition flex items-center gap-2 shrink-0 ${
              activeTab === 'scripts'
                ? 'bg-amber-600 text-white shadow-md'
                : 'text-slate-400 hover:text-white hover:bg-slate-900'
            }`}
          >
            <Code className="w-4 h-4" />
            <span>ЧАСТЬ 4. C# Скрипты</span>
          </button>

          <button
            onClick={() => setActiveTab('dialogues')}
            className={`px-3.5 py-2 rounded-xl text-xs font-bold transition flex items-center gap-2 shrink-0 ${
              activeTab === 'dialogues'
                ? 'bg-cyan-600 text-white shadow-md'
                : 'text-slate-400 hover:text-white hover:bg-slate-900'
            }`}
          >
            <MessageSquare className="w-4 h-4" />
            <span>ЧАСТЬ 5. Шаги и Диалог Кота</span>
          </button>

          <button
            onClick={() => setActiveTab('assets')}
            className={`px-3.5 py-2 rounded-xl text-xs font-bold transition flex items-center gap-2 shrink-0 ${
              activeTab === 'assets'
                ? 'bg-pink-600 text-white shadow-md'
                : 'text-slate-400 hover:text-white hover:bg-slate-900'
            }`}
          >
            <ImageIcon className="w-4 h-4" />
            <span>ЧАСТЬ 1. Промпты Графики</span>
          </button>
        </div>
      </header>

      {/* Main Content Area */}
      <main className="flex-1 p-4 md:p-6 max-w-7xl mx-auto w-full">
        {activeTab === 'fishing_sim' && <FishingSimulator />}
        {activeTab === 'mouse_sim' && <CatchMouseSimulator onOpenFishing={() => setActiveTab('fishing_sim')} />}
        {activeTab === 'hierarchy_guide' && <UnityHierarchyGuide />}
        {activeTab === 'drop_table' && <DropRatesTable />}
        {activeTab === 'scripts' && <ScriptsViewer />}
        {activeTab === 'dialogues' && <CatDialogueViewer />}
        {activeTab === 'assets' && <AssetGallery />}
      </main>

      {/* Footer */}
      <footer className="border-t border-slate-900 bg-slate-950 py-4 text-center text-xs text-slate-500">
        Кот-Алхимик: Studio v18.12.45 • Полная поддержка мобильных устройств и 4K мониторов • C# Unity Engine
      </footer>
    </div>
  );
};

export default App;
