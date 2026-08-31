import React, { useState } from 'react';
import { Copy, Check, Layers, Code, Sparkles, Sliders, Box, MousePointerClick } from 'lucide-react';

export const UnityHierarchyGuide: React.FC = () => {
  const [copiedSection, setCopiedSection] = useState<string | null>(null);

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
      {/* Header Info */}
      <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-6">
        <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-amber-500/20 border border-amber-500/40 flex items-center justify-center text-amber-400">
              <Layers className="w-5 h-5" />
            </div>
            <div>
              <h2 className="text-xl font-bold text-white">ЧАСТЬ 2. Настройка объектов в Unity Hierarchy</h2>
              <p className="text-xs text-slate-400 mt-0.5">
                Иерархия интерфейса Canvas: Панель ловли мышей и Панель Алхимической Рыбалки
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

        {/* Tree Code Box */}
        <div className="mt-4 bg-slate-950 rounded-xl p-4 border border-slate-800 font-mono text-xs text-emerald-400 overflow-x-auto whitespace-pre leading-relaxed shadow-inner">
          {hierarchyText}
        </div>
      </div>

      {/* Animation & Mechanics Details */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* Rod Click & Animation */}
        <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-5 flex flex-col gap-3">
          <div className="flex items-center gap-2.5 text-amber-400 font-bold text-sm">
            <MousePointerClick className="w-4 h-4" />
            <span>Механика клика по удочке и анимация заброса</span>
          </div>
          <ul className="text-xs text-slate-300 space-y-2 leading-relaxed">
            <li className="flex items-start gap-2">
              <span className="text-amber-400 font-bold">1.</span>
              <span>
                <strong>Расположение FishRod_Visual:</strong> Спрайт удочки вешается в правом нижнем углу экрана (где находится аватар алхимика).
              </span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-amber-400 font-bold">2.</span>
              <span>
                <strong>Анимация заброса:</strong> Делается простым покачиванием удочки через <code>LeanTween.rotateZ(gameObject, -15f, 0.2f)</code> или корутину <code>AnimateRodCast()</code> с возвратом в 0°.
              </span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-amber-400 font-bold">3.</span>
              <span>
                <strong>Полет поплавка:</strong> Поплавок вылетает по плавной дуге в центр пруда, где при поклевке начинает покачиваться вверх-вниз с легкими волнами (эффект всплеска).
              </span>
            </li>
          </ul>
        </div>

        {/* Sequential Bars Trigger Flow */}
        <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-5 flex flex-col gap-3">
          <div className="flex items-center gap-2.5 text-purple-400 font-bold text-sm">
            <Sliders className="w-4 h-4" />
            <span>Последовательное появление двух шкал</span>
          </div>
          <ul className="text-xs text-slate-300 space-y-2 leading-relaxed">
            <li className="flex items-start gap-2">
              <span className="text-purple-400 font-bold">1.</span>
              <span>
                <strong>Клик по удочке:</strong> Включает <code>Vertical_Cast_Bar</code> (активирует вертикальный кристалл), горизонтальная шкала пока выключена.
              </span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-purple-400 font-bold">2.</span>
              <span>
                <strong>Остановка шкалы 1:</strong> Игрок кликает и фиксирует дальность. Вертикальная шкала выключается, и моментально появляется <code>Horizontal_Catch_Bar</code> с движущимся фиолетовым лучом.
              </span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-purple-400 font-bold">3.</span>
              <span>
                <strong>Остановка шкалы 2:</strong> Второй клик фиксирует центр. Шкалы скрываются, запускается расчет улова и открывается окно наград <code>Result_Popup_Panel</code>.
              </span>
            </li>
          </ul>
        </div>
      </div>
    </div>
  );
};
