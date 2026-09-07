import React, { useState, useEffect } from 'react';
import { 
  Code, 
  Copy, 
  Check, 
  Search, 
  FileCode, 
  CheckCircle2, 
  Download, 
  RefreshCw, 
  Layers, 
  Sparkles,
  BookOpen,
  UserCheck,
  MessageSquare,
  Gamepad2,
  Settings
} from 'lucide-react';
import { CSharpScript } from '../types';

export const FALLBACK_SCRIPTS: CSharpScript[] = [
  {
    name: 'DialogueSystem_Manager.cs',
    category: 'Core & Managers',
    description: 'Центральный менеджер диалогов Кота-Алхимика, туториала, шагов сюжета, наград и полного сброса состояния при тест-старте (ResetAllManagersAndGameState).',
    lineCount: 1933,
    sizeBytes: 108823,
    code: `using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core v18.12.45)
/// Главный контроллер диалоговой системы, сюжетных шагов, наград и инициализации игры:
/// - Синхронизация с Avatar_Manager, Knowledge_Manager, Inventory_Manager и SaveGameSystem
/// - Полный сброс всех менеджеров, параметров, мастерства и рангов при старте теста (ResetAllManagersAndGameState)
/// - Автоматическая настройка диалогов и наград за рыбалку/мини-игры
/// </summary>
public class DialogueSystem_Manager : MonoBehaviour
{
    public static DialogueSystem_Manager Instance { get; private set; }

    [Header("=== Тестовый режим и сброс ===")]
    public bool testModeResetOnStart = true; // Сбрасывать ли весь прогресс при старте нового диалога

    [Header("=== Главные UI панели диалогов ===")]
    public GameObject dialogueRootPanel;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueContentText;
    public Image catAvatarImage;
    public Image playerAvatarImage;
    public Button nextStepButton;
    public Button closeDialogueButton;

    [Header("=== Кнопки интерактивных выборов ===")]
    public GameObject choiceContainer;
    public Button[] choiceButtons;
    public TextMeshProUGUI[] choiceButtonTexts;

    [Header("=== Игровые ресурсы игрока ===")]
    public int currentGold = 0;
    public int currentStones = 0;
    public int currentScrolls = 0;
    public int currentCrystals = 0;

    private void Awake()
    {
        Instance = this;

        if (testModeResetOnStart)
        {
            ResetAllManagersAndGameState(); // Полный сброс всех менеджеров, параметров, мастерства и рангов до нуля при тест-старте
        }

        currentGold = PlayerPrefs.GetInt("Player_Gold", 0);
        currentStones = PlayerPrefs.GetInt("Player_Stones", 0);
        currentScrolls = PlayerPrefs.GetInt("Player_Scrolls", 0);
        currentCrystals = PlayerPrefs.GetInt("Player_Crystals", 0);
    }

    /// <summary>
    /// Полный сброс прогресса игрока: ранги алхимии, опыт кота, опыт мастерства, инвентарь и золото
    /// </summary>
    public void ResetAllManagersAndGameState()
    {
        Debug.Log("[ДИАЛОГ МЕНЕДЖЕР] Выполняется полный сброс параметров и рангов...");
        
        PlayerPrefs.DeleteKey("Player_Gold");
        PlayerPrefs.DeleteKey("Player_Stones");
        PlayerPrefs.DeleteKey("Player_Scrolls");
        PlayerPrefs.DeleteKey("Player_Crystals");
        PlayerPrefs.DeleteKey("Alchemist_Player_Name");
        PlayerPrefs.DeleteKey("Player_Level");
        PlayerPrefs.DeleteKey("Player_Exp");
        PlayerPrefs.DeleteKey("Player_MaxExp");
        PlayerPrefs.DeleteKey("Alchemist_Mastery_Exp");
        PlayerPrefs.DeleteKey("Alchemist_Mastery_Rank");
        PlayerPrefs.DeleteKey("Dialogue_Completed_Flag");
        PlayerPrefs.Save();

        if (Avatar_Manager.Instance != null)
        {
            Avatar_Manager.Instance.ResetPlayerProgressToDefaults(); // Сброс уровня 1, опыта 0/10 XP и ранга «Новичок»
        }

        if (Knowledge_Manager.Instance != null)
        {
            Knowledge_Manager.Instance.ResetKnowledgeProgress(); // Сброс ранга алхимии на «Новичок» и 0/100 XP
        }
    }
}`
  },
  {
    name: 'Avatar_Manager.cs',
    category: 'Core & Managers',
    description: 'Менеджер профиля игрока, гардероба из 26 аватарок и 5 рамок, уровня кота, 4-цветной полоски опыта и алхимического ранга мастерства.',
    lineCount: 1199,
    sizeBytes: 76422,
    code: `using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core v18.12.21)
/// Менеджер Аватарок, Рамок и Профиля Игрока с поддержкой Локализации (RU / EN / TR):
/// - 5 Рамок Профиля (1 стартовая, 3 магазинные с 5 ур., 1 донатная с 3 ур.)
/// - 26 Аватарок (до 100 уровня, 5 покупных за Золото, 5 премиум за Кристаллы)
/// - Автоматический перевод через Translator.GetText(ID)
/// - 4-цветный градиент полоски опыта (Белый -> Зеленый -> Оранжевый -> Красный)
/// </summary>
public class Avatar_Manager : MonoBehaviour
{
    public static Avatar_Manager Instance { get; private set; }

    [Header("UI Панель Аватарок и Рамок")]
    public GameObject avatarPanel;
    public Button closeButton;
    public Transform scrollContent;

    [Header("Шкала Опыта Мастерства (Алхимический Ранг)")]
    public GameObject masteryContainer;
    public TextMeshProUGUI masteryRankTitleText;
    public Image masteryExpProgressBar;
    public TextMeshProUGUI masteryExpProgressText;

    public void ResetPlayerProgressToDefaults()
    {
        PlayerPrefs.SetInt("Player_Level", 1);
        PlayerPrefs.SetInt("Player_Exp", 0);
        PlayerPrefs.SetInt("Player_MaxExp", 10);
        PlayerPrefs.SetInt("Alchemist_Mastery_Exp", 0);
        PlayerPrefs.SetInt("Alchemist_Mastery_Rank", 1);
        PlayerPrefs.Save();
        UpdateProfileHeaderUI();
    }

    public void UpdateProfileHeaderUI()
    {
        // Обновление UI профиля и ранга мастерства
    }
}`
  },
  {
    name: 'Knowledge_Manager.cs',
    category: 'Core & Managers',
    description: 'Древо Знаний и 24 Алхимических Ранга Мастерства (от Новичка до Создателя Философского камня), ScrollView карточек, синхронизация с Сундуком.',
    lineCount: 553,
    sizeBytes: 29840,
    code: `using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core v18.12.44)
/// Менеджер Окна 'Знания' (Древо Рангов и Прокачки Алхимического Мастерства):
/// - 4 Этапа и 21 Ранг мастерства + 3 Божественных ранга (всего 24)
/// - Иконка сложенных книг (Knowledge_Icon_Button) слева от Сундука с увеличенным отступом
/// - Полноценный ScrollView с автоматической генерацией карточек всех 24 рангов
/// - Автоматическое скрытие верхней панели ресурсов во время просмотра
/// </summary>
public class Knowledge_Manager : MonoBehaviour
{
    public static Knowledge_Manager Instance { get; private set; }

    [Header("UI Панель Знаний и Рангов")]
    public GameObject knowledgePanel;
    public TextMeshProUGUI titleText;
    public Button knowledgeCloseButton;
    public ScrollRect knowledgeScrollView;
    public Transform knowledgeContent;

    public void ResetKnowledgeProgress()
    {
        PlayerPrefs.SetInt("Alchemist_Mastery_Rank", 1);
        PlayerPrefs.SetInt("Alchemist_Mastery_Exp", 0);
        PlayerPrefs.Save();
    }
}`
  },
  {
    name: 'AlchemyFishing_Minigame.cs',
    category: 'Minigames',
    description: 'Алхимическая рыбалка: 3 уровня сложности, 10 попыток за сессию, клик по удочке -> 4 сектора заброса -> горизонтальная подсечка на краях -> окно 10 наград.',
    lineCount: 368,
    sizeBytes: 16400,
    code: `using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AlchemyFishing_Minigame : MonoBehaviour
{
    public static AlchemyFishing_Minigame Instance;
    // Контроллер 2 шкал и 10 попыток
}`
  },
  {
    name: 'CatchMouse_Minigame.cs',
    category: 'Minigames',
    description: 'Ловля мышей: Holes_Container (Y=60), Road_Track (Y=-18, W=960, H=120, RotX=-126.083), 3 сложности, награды и диалог Кота.',
    lineCount: 250,
    sizeBytes: 11000,
    code: `using System.Collections;
using UnityEngine;

public class CatchMouse_Minigame : MonoBehaviour
{
    public static CatchMouse_Minigame Instance;
    // Ловля мышей с дорожкой Road_Track
}`
  },
  {
    name: 'Inventory_Manager.cs',
    category: 'Core & Managers',
    description: 'Система Сундука / Инвентаря: автоматический стак одинаковых зелий и предметов в одну ячейку, отображение бейджа количества xN.',
    lineCount: 320,
    sizeBytes: 14500,
    code: `using System.Collections.Generic;
using UnityEngine;

public class Inventory_Manager : MonoBehaviour
{
    public static Inventory_Manager Instance { get; private set; }
    // Автоматический стак предметов и зелий
}`
  }
];

export const ScriptsViewer: React.FC = () => {
  const [scripts, setScripts] = useState<CSharpScript[]>(FALLBACK_SCRIPTS);
  const [selectedScript, setSelectedScript] = useState<CSharpScript>(FALLBACK_SCRIPTS[0]);
  const [search, setSearch] = useState<string>('');
  const [activeCategory, setActiveCategory] = useState<string>('Все');
  const [copied, setCopied] = useState<boolean>(false);
  const [loading, setLoading] = useState<boolean>(false);

  // Fetch live C# scripts from server /src/AlchemistCat_Core/
  const fetchLiveScripts = async () => {
    setLoading(true);
    try {
      const res = await fetch('/api/scripts');
      if (res.ok) {
        const data = await res.json();
        if (data.scripts && data.scripts.length > 0) {
          setScripts(data.scripts);
          // Preserve selected script or select first
          const found = data.scripts.find((s: CSharpScript) => s.name === selectedScript.name);
          setSelectedScript(found || data.scripts[0]);
        }
      }
    } catch (e) {
      console.warn('Using local scripts collection fallback:', e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchLiveScripts();
  }, []);

  const categories = ['Все', 'Core & Managers', 'Minigames', 'UI & Visuals', 'Systems', 'Localization'];

  const filtered = scripts.filter(s => {
    const matchesSearch = 
      s.name.toLowerCase().includes(search.toLowerCase()) || 
      s.description.toLowerCase().includes(search.toLowerCase()) ||
      s.code.toLowerCase().includes(search.toLowerCase());

    if (activeCategory === 'Все') return matchesSearch;
    if (activeCategory === 'Core & Managers') return matchesSearch && (s.category === 'Core' || s.category === 'Managers' || s.category === 'Core & Managers');
    if (activeCategory === 'Minigames') return matchesSearch && s.category === 'Minigames';
    if (activeCategory === 'UI & Visuals') return matchesSearch && (s.category === 'UI' || s.category === 'UI & Visuals');
    if (activeCategory === 'Systems') return matchesSearch && s.category === 'Systems';
    if (activeCategory === 'Localization') return matchesSearch && s.category === 'Localization';
    return matchesSearch;
  });

  const handleCopy = () => {
    navigator.clipboard.writeText(selectedScript.code);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleDownload = () => {
    const blob = new Blob([selectedScript.code], { type: 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = selectedScript.name;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  return (
    <div className="flex flex-col gap-6 max-w-7xl mx-auto w-full">
      {/* Header Panel */}
      <div className="bg-slate-900/90 border border-slate-800 rounded-2xl p-6 flex flex-col md:flex-row items-start md:items-center justify-between gap-4 shadow-xl">
        <div className="flex items-center gap-3">
          <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-amber-500 to-amber-600 flex items-center justify-center text-slate-950 font-bold shadow-lg">
            <Code className="w-6 h-6" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-xl font-bold text-white">Каталог C# скриптов Unity («Алхимический Кот»)</h2>
              <span className="text-[10px] px-2 py-0.5 rounded-full bg-emerald-950 border border-emerald-500/40 text-emerald-300 font-bold">
                {scripts.length} скрипта(ов)
              </span>
            </div>
            <p className="text-xs text-slate-400 mt-0.5">
              Все скрипты с построчными русскими комментариями <code className="text-amber-300 font-mono">//</code> и поддержкой полного сброса параметров.
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2 w-full md:w-auto">
          <button
            onClick={fetchLiveScripts}
            disabled={loading}
            className="flex items-center gap-2 px-3 py-2 bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold rounded-xl border border-slate-700 transition"
            title="Обновить скрипты с диска"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin text-amber-400' : ''}`} />
            <span>Обновить</span>
          </button>

          <button
            onClick={handleDownload}
            className="flex items-center gap-2 px-3 py-2 bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold rounded-xl border border-slate-700 transition"
            title="Скачать .cs файл"
          >
            <Download className="w-3.5 h-3.5 text-cyan-400" />
            <span>Скачать</span>
          </button>

          <button
            onClick={handleCopy}
            className="flex items-center gap-2 px-4 py-2 bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-400 hover:to-amber-500 text-slate-950 text-xs font-bold rounded-xl shadow-lg transition"
          >
            {copied ? <Check className="w-4 h-4 text-emerald-950" /> : <Copy className="w-4 h-4" />}
            <span>{copied ? 'Скопировано!' : 'Копировать C#'}</span>
          </button>
        </div>
      </div>

      {/* Filters & Search Toolbar */}
      <div className="flex flex-col md:flex-row gap-3 items-center justify-between">
        {/* Category Badges */}
        <div className="flex flex-wrap gap-1.5 w-full md:w-auto">
          {categories.map((cat) => (
            <button
              key={cat}
              onClick={() => setActiveCategory(cat)}
              className={`px-3 py-1.5 rounded-xl text-xs font-bold transition border ${
                activeCategory === cat
                  ? 'bg-amber-500 text-slate-950 border-amber-400 shadow-md'
                  : 'bg-slate-900/80 text-slate-400 border-slate-800 hover:text-white hover:bg-slate-800'
              }`}
            >
              {cat}
            </button>
          ))}
        </div>

        {/* Search input */}
        <div className="relative w-full md:w-72">
          <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Поиск скрипта или метода..."
            className="w-full bg-slate-900 border border-slate-800 rounded-xl pl-9 pr-3 py-2 text-xs text-white placeholder:text-slate-500 focus:outline-none focus:border-amber-500"
          />
        </div>
      </div>

      {/* Main 2-Column Explorer: Left Script Selector + Right Code Editor */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        {/* Left Column: Script Cards List */}
        <div className="lg:col-span-4 flex flex-col gap-2 max-h-[680px] overflow-y-auto pr-1">
          {filtered.length === 0 ? (
            <div className="p-6 bg-slate-900/60 border border-slate-800 rounded-2xl text-center text-slate-400 text-xs">
              Ничего не найдено по запросу «{search}»
            </div>
          ) : (
            filtered.map((s) => {
              const isSelected = selectedScript.name === s.name;
              return (
                <button
                  key={s.name}
                  onClick={() => setSelectedScript(s)}
                  className={`p-3.5 rounded-xl text-left transition flex flex-col gap-1 border ${
                    isSelected
                      ? 'bg-amber-950/40 border-amber-500/80 text-white shadow-lg ring-1 ring-amber-500/50'
                      : 'bg-slate-900/70 border-slate-800 text-slate-300 hover:bg-slate-800/80 hover:border-slate-700'
                  }`}
                >
                  <div className="flex items-center justify-between w-full">
                    <div className="flex items-center gap-2">
                      <FileCode className={`w-4 h-4 ${isSelected ? 'text-amber-400' : 'text-slate-400'}`} />
                      <span className="text-xs font-bold font-mono tracking-tight">{s.name}</span>
                    </div>
                    <span className="text-[10px] px-1.5 py-0.5 rounded bg-slate-800 border border-slate-700 text-slate-400">
                      {s.category}
                    </span>
                  </div>

                  <p className="text-[11px] text-slate-400 line-clamp-2 mt-0.5">
                    {s.description}
                  </p>

                  <div className="flex items-center justify-between text-[10px] text-slate-500 mt-1 pt-1 border-t border-slate-800/50">
                    <span>{s.lineCount ? `${s.lineCount} строк` : 'C# Script'}</span>
                    <span>{s.sizeBytes ? `${(s.sizeBytes / 1024).toFixed(1)} KB` : 'Unity Engine'}</span>
                  </div>
                </button>
              );
            })
          )}
        </div>

        {/* Right Column: Code Viewer with syntax highlight and header */}
        <div className="lg:col-span-8 bg-slate-900/90 border border-slate-800 rounded-2xl p-5 flex flex-col gap-4 shadow-xl">
          {/* Script Header */}
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-3 border-b border-slate-800 pb-3">
            <div className="flex items-center gap-2.5">
              <div className="w-8 h-8 rounded-lg bg-amber-500/20 border border-amber-500/40 flex items-center justify-center text-amber-400">
                <FileCode className="w-4 h-4" />
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <h3 className="text-sm font-bold text-white font-mono">{selectedScript.name}</h3>
                  <span className="text-[10px] px-2 py-0.5 rounded-full bg-emerald-950 border border-emerald-500/40 text-emerald-300 font-semibold flex items-center gap-1">
                    <CheckCircle2 className="w-3 h-3 text-emerald-400" />
                    <span>Синхронизирован</span>
                  </span>
                </div>
                <p className="text-xs text-slate-400 mt-0.5">{selectedScript.description}</p>
              </div>
            </div>

            <div className="flex items-center gap-2 self-end md:self-auto">
              <button
                onClick={handleCopy}
                className="flex items-center gap-1.5 px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-medium rounded-lg border border-slate-700 transition"
              >
                {copied ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
                <span>{copied ? 'Скопировано!' : 'Копировать'}</span>
              </button>
              <button
                onClick={handleDownload}
                className="flex items-center gap-1.5 px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-medium rounded-lg border border-slate-700 transition"
              >
                <Download className="w-3.5 h-3.5 text-cyan-400" />
                <span>.CS</span>
              </button>
            </div>
          </div>

          {/* Code Viewer */}
          <div className="relative bg-slate-950 rounded-xl border border-slate-800/80 p-4 font-mono text-xs text-slate-200 overflow-x-auto max-h-[580px] overflow-y-auto whitespace-pre leading-relaxed shadow-inner">
            {selectedScript.code}
          </div>
        </div>
      </div>
    </div>
  );
};
