import React, { useState } from 'react';
import { Copy, Check, Layers, Code, Sparkles, Sliders, Box, MousePointerClick, CheckCircle2, ChevronRight, Monitor, Package, Award, HelpCircle } from 'lucide-react';

export const UnityHierarchyGuide: React.FC = () => {
  const [copiedSection, setCopiedSection] = useState<string | null>(null);
  const [selectedCategory, setSelectedCategory] = useState<'all' | 'fishing' | 'hidden_object' | 'inventory' | 'scripts' | 'resolution'>('all');

  const copyToClipboard = (text: string, id: string) => {
    navigator.clipboard.writeText(text);
    setCopiedSection(id);
    setTimeout(() => setCopiedSection(null), 2000);
  };

  const hierarchyText = `Canvas (Canvas Scaler: Scale With Screen Size, Ref: 1920x1080 / 3840x2160, Match: 0.5f)
 ├── MinigamesPanel (Anchor: Stretch All [0,0,0,0])
 │    │
 │    ├── CatchMouse_Game_Panel                -> Ловля мышей (Holes Y=60, Road Y=-18, RotX=-126.083)
 │    │    ├── Holes_Container                 -> Pos Y = 60, Width = 1000, Height = 150
 │    │    ├── Road_Track                      -> Pos Y = -18, Width = 960, Height = 120, Rotation X = -126.083°
 │    │    ├── Close_Button                    -> Крестик сверху слева (HandleCloseButtonClicked)
 │    │    └── Reward_Popup_Panel              -> Окно наград ловли
 │    │
 │    ├── AlchemyFishing_Game_Panel            -> Главная панель Алхимической Рыбалки
 │    │    ├── Difficulty_Selection_Panel      -> Выбор сложности (Easy, Medium, Hard)
 │    │    │    ├── Cat_Intro_Dialog           -> Реплики Кота с описанием наград и стаков
 │    │    │    ├── Easy_Level_Card            -> +3000G, +3 Камня, +1 Свиток (10 попыток, Зона 4: 35%)
 │    │    │    ├── Medium_Level_Card          -> +5000G, +5 Камней, +2 Свитка (10 попыток, Зона 4: 22%)
 │    │    │    └── Hard_Level_Card            -> +10000G, +10 Камней, +5 Свитков, Зелье Мастерства (+100 XP)
 │    │    │
 │    │    ├── Active_Fishing_Stage            -> Игровая сцена рыбалки (по умолчанию неактивна)
 │    │    │    ├── Background_Pond            -> Фон пруда (Anchor: Stretch All, Image: Aspect Ratio Fit/Envelope)
 │    │    │    ├── Close_Button               -> Крестик сверху слева (возврат в меню)
 │    │    │    ├── Attempts_Badge             -> Бейдж «Попытка: X / 10»
 │    │    │    ├── Water_Surface_TargetArea   -> Зона всплеска и плавающий поплавок
 │    │    │    ├── FishRod_Visual_Button      -> Удочка в правом нижнем углу (триггер старта)
 │    │    │    │
 │    │    │    ├── Vertical_Cast_Bar          -> Шкала 1: Дальность заброса (4 сектора, 3 разделителя)
 │    │    │    │    ├── Bar_Background_Frame
 │    │    │    │    ├── Zone4_SweetSpot_Fill  -> Верхний сектор (динамический: 35%/22%/12%)
 │    │    │    │    ├── Delimiter_Lines (3 шт)-> Разделители секторов 1, 2, 3, 4
 │    │    │    │    └── Slider_Arrow_Cursor   -> Движущийся бегунок
 │    │    │    │
 │    │    │    ├── Horizontal_Catch_Bar       -> Шкала 2: Поклевка (появляется после шкалы 1)
 │    │    │    │    ├── Horizontal_Frame
 │    │    │    │    ├── Center_Anchor_Glow    -> Центральный маркер
 │    │    │    │    ├── Left_Beam_Marker      -> Луч влево
 │    │    │    │    └── Right_Beam_Marker     -> Луч вправо (подсечка на краях)
 │    │    │    │
 │    │    │    └── Action_Main_Button         -> Большая кнопка действия внизу экрана
 │    │    │
 │    │    └── Result_Summary_Popup_Panel      -> Итоговое окно 10 попыток
 │    │         ├── Trophy_Header              -> Заголовок «Сессия рыбалки завершена!»
 │    │         ├── Stacked_Inventory_Box      -> Сундук со стаками одинаковых зелий (x2, x3...)
 │    │         ├── Guaranteed_Rewards_Box     -> Начисленное золото, камни, свитки и зелье
 │    │         └── Claim_All_Button           -> «Забрать все в рюкзак» (старт диалога поиска)
 │    │
 │    └── HiddenObject_Game_Panel              -> Новая игра: Поиск предметов в 3 локациях (8K Fullscreen)
 │         ├── Location_Selection_Popup        -> Выбор локации: 1.Лавка, 2.Дом, 3.Рынок
 │         ├── Fullscreen_Viewport_Container   -> Контейнер зума и панорамирования (Stretch All)
 │         │    └── Background_Content_Root    -> Масштабируемый корень с 8K фоном
 │         │         ├── Background_8K_Image   -> Изометрический 8K арт во весь экран (AspectRatioFitter)
 │         │         └── Interactive_Click_Targets -> Скрытые коллайдеры/кнопки поиска предметов
 │         ├── Top_Status_Bar                  -> Таймер (180 сек), Кнопка подсказки Кота, Выход
 │         ├── Bottom_Targets_Bar              -> Сетка иконок предметов для поиска со счетчиком
 │         └── Victory_Location_Popup          -> Награды: +5000G, +3 Камня, +2 Свитка в сундук
 │
 └── Inventory_Chest_Panel                     -> Панель Сундука / Инвентаря игрока
      ├── Background_Dimmer
      ├── Chest_Header                         -> "Сундук Алхимика"
      ├── Chest_Slots_Container (GridLayout)   -> Сетка слотов (CellSize=110x120, Spacing=12x12)
      │    └── Chest_Slot_Prefab
      │         ├── Slot_Background
      │         ├── Item_Icon                  -> Спрайт зелья
      │         ├── Count_Badge                -> Бейдж количества в углу
      │         │    └── Text                  -> "x3", "x5" (отображается при count > 1)
      │         ├── Item_Title                 -> Название предмета с цветом редкости
      │         └── Rarity_Border              -> Цветная рамка качества
      └── Total_Chest_Stats_Bar                -> Итого предметов и суммарный опыт`;

  return (
    <div className="flex flex-col gap-6 max-w-5xl mx-auto">
      {/* Category Filter */}
      <div className="flex items-center gap-2 overflow-x-auto no-scrollbar pb-1">
        <button
          onClick={() => setSelectedCategory('all')}
          className={`px-3 py-1.5 rounded-xl text-xs font-bold transition whitespace-nowrap ${
            selectedCategory === 'all' ? 'bg-amber-500 text-slate-950 shadow' : 'bg-slate-900 text-slate-400 hover:text-white'
          }`}
        >
          Все разделы
        </button>
        <button
          onClick={() => setSelectedCategory('fishing')}
          className={`px-3 py-1.5 rounded-xl text-xs font-bold transition whitespace-nowrap ${
            selectedCategory === 'fishing' ? 'bg-emerald-600 text-white shadow' : 'bg-slate-900 text-slate-400 hover:text-white'
          }`}
        >
          🎣 Шаги 3, 4, 5 (Рыбалка & Шкалы)
        </button>
        <button
          onClick={() => setSelectedCategory('hidden_object')}
          className={`px-3 py-1.5 rounded-xl text-xs font-bold transition whitespace-nowrap ${
            selectedCategory === 'hidden_object' ? 'bg-purple-600 text-white shadow' : 'bg-slate-900 text-slate-400 hover:text-white'
          }`}
        >
          🔍 HiddenObject_Game_Panel (Поиск предметов)
        </button>
        <button
          onClick={() => setSelectedCategory('inventory')}
          className={`px-3 py-1.5 rounded-xl text-xs font-bold transition whitespace-nowrap ${
            selectedCategory === 'inventory' ? 'bg-blue-600 text-white shadow' : 'bg-slate-900 text-slate-400 hover:text-white'
          }`}
        >
          📦 Inventory_Chest_Panel (Сундук со стаками)
        </button>
        <button
          onClick={() => setSelectedCategory('scripts')}
          className={`px-3 py-1.5 rounded-xl text-xs font-bold transition whitespace-nowrap ${
            selectedCategory === 'scripts' ? 'bg-amber-600 text-white shadow' : 'bg-slate-900 text-slate-400 hover:text-white'
          }`}
        >
          📋 Привязка скриптов в Инспекторе
        </button>
        <button
          onClick={() => setSelectedCategory('resolution')}
          className={`px-3 py-1.5 rounded-xl text-xs font-bold transition whitespace-nowrap ${
            selectedCategory === 'resolution' ? 'bg-teal-600 text-white shadow' : 'bg-slate-900 text-slate-400 hover:text-white'
          }`}
        >
          🖥️ 4K & Настройка соотношения сторон
        </button>
      </div>

      {/* Main Hierarchy Box */}
      {(selectedCategory === 'all' || selectedCategory === 'fishing') && (
        <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-6 shadow-xl">
          <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-amber-500/20 border border-amber-500/40 flex items-center justify-center text-amber-400">
                <Layers className="w-5 h-5" />
              </div>
              <div>
                <h2 className="text-xl font-bold text-white">Полная Иерархия Canvas в Unity</h2>
                <p className="text-xs text-slate-400 mt-0.5">
                  Структура объектов для 2 шкал рыбалки, поиска предметов 8K и сундука инвентаря
                </p>
              </div>
            </div>

            <button
              onClick={() => copyToClipboard(hierarchyText, 'hierarchy')}
              className="flex items-center gap-2 px-4 py-2 bg-amber-500 hover:bg-amber-400 text-slate-950 text-xs font-bold rounded-xl transition"
            >
              {copiedSection === 'hierarchy' ? <Check className="w-4 h-4" /> : <Copy className="w-4 h-4 text-slate-950" />}
              <span>Скопировать дерево Hierarchy</span>
            </button>
          </div>

          <div className="mt-4 bg-slate-950 rounded-xl p-4 border border-slate-800 font-mono text-xs text-emerald-400 overflow-x-auto whitespace-pre leading-relaxed shadow-inner">
            {hierarchyText}
          </div>
        </div>
      )}

      {/* Step 3: Vertical Cast Bar */}
      {(selectedCategory === 'all' || selectedCategory === 'fishing') && (
        <div className="bg-slate-900/90 border border-emerald-500/30 rounded-2xl p-6 flex flex-col gap-4 shadow-lg">
          <div className="flex items-center justify-between border-b border-slate-800 pb-3">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-lg bg-emerald-500/20 border border-emerald-500/40 text-emerald-400 flex items-center justify-center font-bold text-sm">
                3
              </div>
              <div>
                <h3 className="text-base font-bold text-white">Шаг 3. Создание Шкалы 1: Дальность заброса (Vertical_Cast_Bar)</h3>
                <p className="text-xs text-slate-400">Родительский объект: Active_Fishing_Stage</p>
              </div>
            </div>
            <span className="text-xs px-2.5 py-1 bg-emerald-950 border border-emerald-500/40 text-emerald-300 font-semibold rounded-lg">
              Шкала Дальности
            </span>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs">
            <div className="bg-slate-950 p-4 rounded-xl border border-slate-800">
              <h4 className="font-bold text-amber-400 mb-2">1. Параметры RectTransform родителя:</h4>
              <ul className="space-y-1.5 text-slate-300">
                <li>• Создать: ПКМ по <code>Active_Fishing_Stage</code> → <strong>Create Empty</strong> → <code>Vertical_Cast_Bar</code></li>
                <li>• Якорь: <strong>Middle-Left</strong> (слева по центру)</li>
                <li>• <code>Pos X = 90</code>, <code>Pos Y = 0</code></li>
                <li>• <code>Width = 70</code>, <code>Height = 460</code></li>
              </ul>
            </div>

            <div className="bg-slate-950 p-4 rounded-xl border border-slate-800">
              <h4 className="font-bold text-amber-400 mb-2">2. Дочерние элементы внутри:</h4>
              <ul className="space-y-1.5 text-slate-300">
                <li>• <code>Bar_Background_Frame</code>: UI → Image (Stretch All). Спрайт «Вертикальная шкала».</li>
                <li>• <code>Zone4_SweetSpot_Fill</code>: UI → Image (Top-Stretch, Pos Y = 0, Height = 130, золотой/изумрудный).</li>
                <li>• <code>Delimiter_Lines</code>: 3 объекта UI → Image (H = 2, W = 60) на Pos Y = 115, -15, -145.</li>
                <li>• <code>Slider_Arrow_Cursor</code>: UI → Image (W = 65, H = 22, подвижный бегунок).</li>
              </ul>
            </div>
          </div>
        </div>
      )}

      {/* Step 4: Horizontal Catch Bar */}
      {(selectedCategory === 'all' || selectedCategory === 'fishing') && (
        <div className="bg-slate-900/90 border border-purple-500/30 rounded-2xl p-6 flex flex-col gap-4 shadow-lg">
          <div className="flex items-center justify-between border-b border-slate-800 pb-3">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-lg bg-purple-500/20 border border-purple-500/40 text-purple-400 flex items-center justify-center font-bold text-sm">
                4
              </div>
              <div>
                <h3 className="text-base font-bold text-white">Шаг 4. Создание Шкалы 2: Поклевка (Horizontal_Catch_Bar)</h3>
                <p className="text-xs text-slate-400">Родительский объект: Active_Fishing_Stage</p>
              </div>
            </div>
            <span className="text-xs px-2.5 py-1 bg-purple-950 border border-purple-500/40 text-purple-300 font-semibold rounded-lg">
              Шкала Поклевки
            </span>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs">
            <div className="bg-slate-950 p-4 rounded-xl border border-slate-800">
              <h4 className="font-bold text-amber-400 mb-2">1. Параметры RectTransform:</h4>
              <ul className="space-y-1.5 text-slate-300">
                <li>• Создать: ПКМ по <code>Active_Fishing_Stage</code> → <strong>Create Empty</strong> → <code>Horizontal_Catch_Bar</code></li>
                <li>• Якорь: <strong>Top-Center</strong></li>
                <li>• <code>Pos X = 0</code>, <code>Pos Y = -160</code></li>
                <li>• <code>Width = 560</code>, <code>Height = 60</code></li>
              </ul>
            </div>

            <div className="bg-slate-950 p-4 rounded-xl border border-slate-800">
              <h4 className="font-bold text-amber-400 mb-2">2. Дочерние элементы:</h4>
              <ul className="space-y-1.5 text-slate-300">
                <li>• <code>Horizontal_Frame</code>: UI → Image (Stretch All, темная рамка).</li>
                <li>• <code>Center_Anchor_Glow</code>: UI → Image (Middle-Center, Width = 20, Height = 50, яркий маркер).</li>
                <li>• <code>Left_Beam_Marker</code>: UI → Image (W = 30, H = 40, Pos X = -50).</li>
                <li>• <code>Right_Beam_Marker</code>: UI → Image (W = 30, H = 40, Pos X = 50).</li>
              </ul>
            </div>
          </div>
        </div>
      )}

      {/* Step 5: Action Button & Summary Popup */}
      {(selectedCategory === 'all' || selectedCategory === 'fishing') && (
        <div className="bg-slate-900/90 border border-amber-500/30 rounded-2xl p-6 flex flex-col gap-4 shadow-lg">
          <div className="flex items-center justify-between border-b border-slate-800 pb-3">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-lg bg-amber-500/20 border border-amber-500/40 text-amber-400 flex items-center justify-center font-bold text-sm">
                5
              </div>
              <div>
                <h3 className="text-base font-bold text-white">Шаг 5. Кнопка действия и Окно итогов (Result_Summary_Popup_Panel)</h3>
                <p className="text-xs text-slate-400">Action_Main_Button и окно суммарных наград</p>
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs">
            <div className="bg-slate-950 p-4 rounded-xl border border-slate-800">
              <h4 className="font-bold text-amber-400 mb-2">Action_Main_Button:</h4>
              <ul className="space-y-1.5 text-slate-300">
                <li>• ПКМ по <code>Active_Fishing_Stage</code> → <strong>UI → Button - TextMeshPro</strong> → <code>Action_Main_Button</code></li>
                <li>• Якорь: <strong>Bottom-Center</strong>, <code>Pos X = 0</code>, <code>Pos Y = 90</code>, <code>W = 340</code>, <code>H = 80</code></li>
                <li>• Спрайт: <code>Battons_Dialogue</code></li>
                <li>• Текст (TMP): «Забросить удочку» (Font Size = 28, Color = White)</li>
              </ul>
            </div>

            <div className="bg-slate-950 p-4 rounded-xl border border-slate-800">
              <h4 className="font-bold text-amber-400 mb-2">Result_Summary_Popup_Panel:</h4>
              <ul className="space-y-1.5 text-slate-300">
                <li>• ПКМ по <code>AlchemyFishing_Game_Panel</code> → <strong>UI → Panel</strong> → <code>Result_Summary_Popup_Panel</code> (отключить видимость)</li>
                <li>• <code>Trophy_Header</code>: TMP «Сессия рыбалки завершена!» (Top-Center, Pos Y = -120, Size = 36, Gold)</li>
                <li>• <code>Stacked_Inventory_Box</code>: Panel (Middle-Center, Pos Y = 40, W = 720, H = 190) для выловленных стаков</li>
                <li>• <code>Guaranteed_Rewards_Box</code>: Panel (Pos Y = -110, W = 720, H = 100) с 3 полями (Золото, Камни, Свитки)</li>
                <li>• <code>Claim_All_Button</code>: Button-TMP (Bottom-Center, Pos Y = 110, W = 360, H = 75, «Забрать всё в рюкзак»)</li>
              </ul>
            </div>
          </div>
        </div>
      )}

      {/* Hidden Object Section */}
      {(selectedCategory === 'all' || selectedCategory === 'hidden_object') && (
        <div className="bg-slate-900/90 border border-purple-500/30 rounded-2xl p-6 flex flex-col gap-4 shadow-lg">
          <div className="flex items-center justify-between border-b border-slate-800 pb-3">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-lg bg-purple-500/20 border border-purple-500/40 text-purple-400 flex items-center justify-center font-bold text-sm">
                🔍
              </div>
              <div>
                <h3 className="text-base font-bold text-white">Шаг 4. Иерархия HiddenObject_Game_Panel (Поиск предметов в 3 локациях)</h3>
                <p className="text-xs text-slate-400">8K фон, зум, таймер 180 сек, подсказки Кота и победный попап</p>
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-3 text-xs">
            <div className="bg-slate-950 p-4 rounded-xl border border-slate-800">
              <h4 className="font-bold text-purple-400 mb-2">1. Location_Selection_Popup:</h4>
              <p className="text-slate-400 mb-2">Окно выбора 3 локаций с кнопками со спрайтом Battons_Dialogue:</p>
              <ul className="space-y-1 text-slate-300">
                <li>• 1. Алхимическая лавка Кота (PosY=100, W=650, H=80)</li>
                <li>• 2. Старый заброшенный дом (PosY=0, W=650, H=80)</li>
                <li>• 3. Антикварный рынок (PosY=-100, W=650, H=80)</li>
              </ul>
            </div>

            <div className="bg-slate-950 p-4 rounded-xl border border-slate-800">
              <h4 className="font-bold text-purple-400 mb-2">2. Fullscreen_Viewport_Container:</h4>
              <p className="text-slate-400 mb-2">Контейнер масштабирования (Stretch All):</p>
              <ul className="space-y-1 text-slate-300">
                <li>• Компонент <strong>Rect Mask 2D</strong></li>
                <li>• Внутри: <code>Background_Content_Root</code> (Stretch All, Pivot X=0.5, Y=0.5)</li>
                <li>• Внутри: <code>Background_8K_Image</code> с <strong>AspectRatioFitter</strong> (Envelope Parent)</li>
                <li>• Внутри: <code>Interactive_Click_Targets</code> для кликабельных зон предметов</li>
              </ul>
            </div>

            <div className="bg-slate-950 p-4 rounded-xl border border-slate-800">
              <h4 className="font-bold text-purple-400 mb-2">3. Панели интерфейса & Победа:</h4>
              <ul className="space-y-1 text-slate-300">
                <li>• <code>Top_Status_Bar</code>: TimerText («⏳ 03:00»), Hint_Cat_Button («💡 Подсказка: 3»), Close_Button.</li>
                <li>• <code>Bottom_Targets_Bar</code>: ItemsRemainingText («Найдено: 0 / 10») + HorizontalLayoutGroup для иконок.</li>
                <li>• <code>Victory_Location_Popup</code>: Заголовок, начисленные награды (+5000G, +3 Камня, +2 Свитка) и ClaimRewardsAndNextButton.</li>
              </ul>
            </div>
          </div>
        </div>
      )}

      {/* Inventory Chest Section */}
      {(selectedCategory === 'all' || selectedCategory === 'inventory') && (
        <div className="bg-slate-900/90 border border-blue-500/30 rounded-2xl p-6 flex flex-col gap-4 shadow-lg">
          <div className="flex items-center justify-between border-b border-slate-800 pb-3">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-lg bg-blue-500/20 border border-blue-500/40 text-blue-400 flex items-center justify-center font-bold text-sm">
                📦
              </div>
              <div>
                <h3 className="text-base font-bold text-white">Шаг 5. Иерархия Inventory_Chest_Panel (Сундук со стаками x2, x5)</h3>
                <p className="text-xs text-slate-400">Сетка инвентаря с префабом слота и авто-группировкой</p>
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs">
            <div className="bg-slate-950 p-4 rounded-xl border border-slate-800">
              <h4 className="font-bold text-blue-400 mb-2">Структура Inventory_Chest_Panel (на уровне Canvas):</h4>
              <ul className="space-y-1.5 text-slate-300">
                <li>• <code>Background_Dimmer</code>: UI → Image (черный фон, Alpha = 180, Stretch All)</li>
                <li>• <code>Chest_Header</code>: TMP «Сундук Алхимика» (Pos Y = 280, Font Size = 40)</li>
                <li>• <code>Chest_Slots_Container</code>: Panel (Pos Y = 20, W = 740, H = 460) с <strong>Grid Layout Group</strong>:
                  <div className="pl-3 text-slate-400 mt-1">Cell Size: X = 110, Y = 120 | Spacing: X = 12, Y = 12 | Alignment: Upper Center</div>
                </li>
                <li>• <code>Total_Chest_Stats_Bar</code>: TMP «Всего предметов: X | Накоплено опыта: +XXXX XP»</li>
              </ul>
            </div>

            <div className="bg-slate-950 p-4 rounded-xl border border-slate-800">
              <h4 className="font-bold text-blue-400 mb-2">Создание префаба ячейки Chest_Slot_Prefab:</h4>
              <ul className="space-y-1.5 text-slate-300">
                <li>1. Создать внутри Chest_Slots_Container: UI → Image <code>Chest_Slot_Prefab</code> (W = 110, H = 120).</li>
                <li>2. Внутрь добавить <code>Item_Icon</code> (UI → Image зелья).</li>
                <li>3. Внутрь добавить <code>Count_Badge</code> (круглая плашка в правом нижнем углу с TextMeshPro: «x2», «x5»).</li>
                <li>4. Внутрь добавить <code>Item_Title</code> (TMP названия) и <code>Rarity_Border</code> (рамка качества).</li>
                <li>5. <strong>Перетащить</strong> <code>Chest_Slot_Prefab</code> в папку <code>Assets/Prefabs</code> и удалить со сцены.</li>
              </ul>
            </div>
          </div>
        </div>
      )}

      {/* Script Binding Reference */}
      {(selectedCategory === 'all' || selectedCategory === 'scripts') && (
        <div className="bg-slate-900/90 border border-amber-500/30 rounded-2xl p-6 flex flex-col gap-4 shadow-lg">
          <div className="flex items-center justify-between border-b border-slate-800 pb-3">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-lg bg-amber-500/20 border border-amber-500/40 text-amber-400 flex items-center justify-center font-bold text-sm">
                📋
              </div>
              <div>
                <h3 className="text-base font-bold text-white">Часть 3. Справочник: Куда вешать скрипты и привязывать поля</h3>
                <p className="text-xs text-slate-400">Связывание C# компонентов и UI объектов в Unity Inspector</p>
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs">
            <div className="bg-slate-950 p-4 rounded-xl border border-slate-800">
              <h4 className="font-bold text-emerald-400 mb-1">1. AlchemyFishing_Minigame.cs</h4>
              <p className="text-slate-400 text-[11px] mb-2">Вешать на: <code>AlchemyFishing_Game_Panel</code></p>
              <div className="space-y-1 text-slate-300">
                <p>• Difficulty Select Panel → <code>Difficulty_Selection_Panel</code></p>
                <p>• Active Fishing Stage Panel → <code>Active_Fishing_Stage</code></p>
                <p>• Result Summary Popup Panel → <code>Result_Summary_Popup_Panel</code></p>
                <p>• Fish Rod Button → <code>FishRod_Visual_Button</code></p>
                <p>• Vertical Slider Arrow → <code>Slider_Arrow_Cursor</code></p>
                <p>• Left/Right Moving Beam → <code>Left_Beam_Marker</code>, <code>Right_Beam_Marker</code></p>
                <p>• Summary Texts → <code>Guaranteed_Rewards_Box</code> текстовые поля</p>
                <p>• Claim All To Backpack Button → <code>Claim_All_Button</code></p>
              </div>
            </div>

            <div className="bg-slate-950 p-4 rounded-xl border border-slate-800">
              <h4 className="font-bold text-purple-400 mb-1">2. HiddenObject_Minigame.cs</h4>
              <p className="text-slate-400 text-[11px] mb-2">Вешать на: <code>HiddenObject_Game_Panel</code></p>
              <div className="space-y-1 text-slate-300">
                <p>• Hidden Object Panel → <code>HiddenObject_Game_Panel</code></p>
                <p>• Location Select Popup → <code>Location_Selection_Popup</code></p>
                <p>• Close Game Button → <code>Close_Button</code></p>
                <p>• Viewport Container → <code>Fullscreen_Viewport_Container</code></p>
                <p>• Background Content Root → <code>Background_Content_Root</code></p>
                <p>• Background Location Image → <code>Background_8K_Image</code></p>
                <p>• Target Items Container → <code>Bottom_Targets_Bar</code></p>
                <p>• Hint Cat Button → <code>Hint_Cat_Button</code></p>
                <p>• Victory Popup Panel → <code>Victory_Location_Popup</code></p>
              </div>
            </div>

            <div className="bg-slate-950 p-4 rounded-xl border border-slate-800">
              <h4 className="font-bold text-blue-400 mb-1">3. Inventory_Manager.cs</h4>
              <p className="text-slate-400 text-[11px] mb-2">Вешать на: <code>Inventory_Chest_Panel</code> (или <code>_GameSystems</code>)</p>
              <div className="space-y-1 text-slate-300">
                <p>• Chest Inventory Panel → <code>Inventory_Chest_Panel</code></p>
                <p>• Chest Slots Container → <code>Chest_Slots_Container</code></p>
                <p>• Chest Slot Prefab → префаб <code>Chest_Slot_Prefab</code></p>
                <p>• Total Stats Texts → поля из <code>Total_Chest_Stats_Bar</code></p>
              </div>
            </div>

            <div className="bg-slate-950 p-4 rounded-xl border border-slate-800">
              <h4 className="font-bold text-amber-400 mb-1">4. Avatar_Manager.cs</h4>
              <p className="text-slate-400 text-[11px] mb-2">Вешать на: <code>_GameSystems</code></p>
              <div className="space-y-1 text-slate-300">
                <p>• Управляет начислением <code>AddGold</code>, <code>AddStones</code>, <code>AddScrolls</code>, <code>AddCrystals</code>, <code>AddExperience</code>.</p>
                <p>• Автоматически обновляет профиль, уровни и кошелек игрока после мини-игр.</p>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Screen scaling & 4K setup */}
      {(selectedCategory === 'all' || selectedCategory === 'resolution') && (
        <div className="bg-slate-900/90 border border-teal-500/30 rounded-2xl p-6 flex flex-col gap-4 shadow-lg">
          <div className="flex items-center justify-between border-b border-slate-800 pb-3">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-lg bg-teal-500/20 border border-teal-500/40 text-teal-400 flex items-center justify-center font-bold text-sm">
                🖥️
              </div>
              <div>
                <h3 className="text-base font-bold text-white">Пошаговая настройка рендера во всю ширину и высоту (4K ПК и любые телефоны)</h3>
                <p className="text-xs text-slate-400">Без черных полос и искажений (ПК 16:9, 21:9 UltraWide, смартфоны 19.5:9, планшеты 4:3)</p>
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-3 text-xs">
            <div className="bg-slate-950 p-4 rounded-xl border border-slate-800">
              <h4 className="font-bold text-teal-400 mb-2">1. Настройка Canvas Scaler:</h4>
              <ul className="space-y-1 text-slate-300">
                <li>• Выберите объект <strong>Canvas</strong>.</li>
                <li>• UI Scale Mode: <strong>Scale With Screen Size</strong></li>
                <li>• Reference Resolution: <code>1920 x 1080</code> (или <code>3840 x 2160</code>)</li>
                <li>• Screen Match Mode: <strong>Match Width Or Height</strong></li>
                <li>• Match: <strong>0.5</strong></li>
              </ul>
            </div>

            <div className="bg-slate-950 p-4 rounded-xl border border-slate-800">
              <h4 className="font-bold text-teal-400 mb-2">2. Настройка Background_8K_Image:</h4>
              <ul className="space-y-1 text-slate-300">
                <li>• RectTransform: <code>Alt + Shift</code> + <strong>Stretch All</strong> (0, 0, 0, 0)</li>
                <li>• Добавить компонент: <strong>Aspect Ratio Fitter</strong></li>
                <li>• Aspect Mode: <strong>Envelope Parent</strong></li>
                <li>• Aspect Ratio: <code>1.777778</code> (или ПКМ → Set Native Size)</li>
              </ul>
            </div>

            <div className="bg-slate-950 p-4 rounded-xl border border-slate-800">
              <h4 className="font-bold text-teal-400 mb-2">3. Импорт 8K текстуры в Unity:</h4>
              <ul className="space-y-1 text-slate-300">
                <li>• Texture Type: <strong>Sprite (2D and UI)</strong></li>
                <li>• Sprite Mode: <strong>Single</strong></li>
                <li>• Generate Physics Shape: <strong>Снять галочку</strong></li>
                <li>• Wrap Mode: <strong>Clamp</strong> | Filter Mode: <strong>Bilinear</strong></li>
                <li>• Max Size: <strong>4096 или 8192</strong></li>
                <li>• Compression: <strong>High Quality</strong></li>
              </ul>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

