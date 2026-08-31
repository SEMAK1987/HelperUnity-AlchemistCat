import React, { useState } from 'react';
import { Image as ImageIcon, Copy, Check, Sparkles, Download, Layers } from 'lucide-react';

interface AssetPrompt {
  id: string;
  name: string;
  category: 'potions' | 'scales' | 'environment' | 'locations';
  rarityColor: string;
  iconEmoji: string;
  promptRu: string;
  promptEn: string;
  unityImportNote: string;
}

export const ASSETS_PROMPTS: AssetPrompt[] = [
  {
    id: 'alchemy_shop_loc',
    name: '1. Алхимическая лавка Кота (Alchemy Shop - 8K Ultra Detailed)',
    category: 'locations',
    rarityColor: '#10B981',
    iconEmoji: '🧪',
    promptRu: '2D игра изометрический фон в разрезе, магическая фэнтези аптека и алхимическая лавка, деревянные стеллажи с сотнями светящихся колб, книги заклинаний, перегонный аппарат алембик, бурлящий медный котел с искрами, подвешенные сушеные светящиеся травы, уютный теплый свет свечей, россыпь мистических ингредиентов и свитков, 8k разрешение, ультра детализация, художественный стиль фэнтези мобильной игры',
    promptEn: '2D game isometric cutaway background, magical fantasy alchemy apothecary shop interior, wooden shelves stacked with glowing potion bottles, spellbooks, alchemical alembic distillation apparatus, bubbling copper cauldron, hanging dried luminous herbs, cozy warm candle lighting, clutter of mystic ingredients and scrolls, 8k resolution, highly detailed digital painting, mobile game art style',
    unityImportNote: 'Texture Type: Sprite (2D and UI) -> Max Size: 4096 / 8192 -> Compression: High Quality. AspectRatioFitter: EnvelopeParent (во весь экран 4K PC 16:9/21:9 и Mobile 19.5:9/20:9)',
  },
  {
    id: 'old_house_loc',
    name: '2. Старый заброшенный дом (Old Abandoned House - 8K Ultra Detailed)',
    category: 'locations',
    rarityColor: '#F59E0B',
    iconEmoji: '🏚️',
    promptRu: '2D игра изометрический фон в разрезе, таинственный заброшенный чердак древнего мага, пыльные деревянные балки с паутиной, разбитые светящиеся склянки с зельями, старинные кованые сундуки, астрономические телескопы, пергаментные свитки на дубовых столах, лунные лучи пробиваются сквозь проломленную крышу, 8k разрешение, детальная атмосфера, фэнтези стиль мобильной игры',
    promptEn: '2D game isometric cutaway background, mysterious abandoned ancient wizard attic house interior, dusty wooden beams with cobwebs, shattered glowing potion vials, ancient antique chests, old celestial telescopes, parchment scrolls on wooden tables, moonlight beams through broken roof, 8k resolution, detailed atmosphere, fantasy mobile game art style',
    unityImportNote: 'Texture Type: Sprite (2D and UI) -> Fullscreen Viewport -> Pinch-to-zoom & Drag Pan поддержка на мобильных устройствах и ПК',
  },
  {
    id: 'antique_market_loc',
    name: '3. Антикварный рынок (Antique Fantasy Market - 8K Ultra Detailed)',
    category: 'locations',
    rarityColor: '#A855F7',
    iconEmoji: '🏺',
    promptRu: '2D игра изометрический вид, шумная старинная фэнтезийная торговая улица базара в сумерках, нарядные торговые палатки с яркими шелковыми навесами, деревянные ящики с мистическими реликвиями, светящиеся хрустальные фонари, винтажные аптекарские весы, сундуки с самоцветами и свитками, подвешенные алхимические амулеты, 8k разрешение, сочные насыщенные цвета, фэнтези стиль',
    promptEn: '2D game isometric view background, bustling ancient fantasy bazaar market street at twilight, ornate market stalls with colorful silk canopies, wooden crates with mystical relics, glowing crystal lanterns, vintage scales, chests overflowing with gems and scrolls, hanging alchemy charms, 8k resolution, vibrant rich colors, fantasy mobile game art style',
    unityImportNote: 'Texture Type: Sprite (2D and UI) -> Filter: Bilinear -> Canvas Scaler: Scale With Screen Size (Match 0.5f для всех видов экранов)',
  },
  {
    id: 'dragon_potion',
    name: 'Драконье Зелье Опыта (+3000 XP)',
    category: 'potions',
    rarityColor: '#FF4500',
    iconEmoji: '🐉',
    promptRu: 'Иконка зелья для 2D игры, верховный драконий эликсир в сосуде в форме головы дракона, кипящая огненная лава внутри, огненные искры и дымка, золотая оправа с рогами дракона, прозрачный фон, изолированный PNG --no background',
    promptEn: '2D mobile game icon, supreme dragon experience elixir potion, bottle shaped like an ornate mythical dragon head, bubbling glowing molten lava liquid inside, fiery sparks and ember wisps, gold and obsidian dragon horns filigree, crystal glass transparency, isolated png, transparent background, high resolution --no background',
    unityImportNote: 'Sprite (2D and UI) -> RGBA 32 bit -> Wrap: Clamp -> Filter: Bilinear',
  },
  {
    id: 'cosmic_potion',
    name: 'Мифическое Зелье Опыта (+1000 XP)',
    category: 'potions',
    rarityColor: '#8B5CF6',
    iconEmoji: '🌌',
    promptRu: 'Иконка зелья для игры, космический эликсир звездной пыли в сосуде с кольцами Сатурна, мерцающие галактики и туманности внутри, фиолетово-синее свечение, прозрачный фон, изолированный PNG',
    promptEn: '2D mobile game icon, cosmic galaxy potion bottle with planetary rings, swirling nebula starry vortex inside, glowing neon violet and celestial blue luminescence, gem stopper, isolated png, transparent background --no background',
    unityImportNote: 'Sprite (2D and UI) -> Alpha Is Transparency checked',
  },
  {
    id: 'amber_potion',
    name: 'Легендарное Зелье Опыта (+500 XP)',
    category: 'potions',
    rarityColor: '#EAB308',
    iconEmoji: '👑',
    promptRu: 'Иконка зелья, янтарно-золотой эликсир в королевской ограненной колбе, золотая корона-пробка, золотые лучи и мерцающие частицы, прозрачный фон PNG',
    promptEn: '2D mobile game icon, legendary golden elixir potion in royal faceted crystal decanter, ornate crown stopper, glowing liquid amber gold with radiant light rays, isolated png, transparent background --no background',
    unityImportNote: 'Sprite (2D and UI) -> Pixels Per Unit: 100',
  },
  {
    id: 'ruby_potion',
    name: 'Магическое Зелье Опыта (+300 XP)',
    category: 'potions',
    rarityColor: '#F43F5E',
    iconEmoji: '🔮',
    promptRu: 'Иконка зелья, рубиновая круглая колба с огненным мерцанием, крылья феникса на оправе, прозрачный фон PNG',
    promptEn: '2D fantasy game asset, magical ruby experience potion in round flask, glowing crimson red liquid, phoenix feather filigree, isolated png, transparent background --no background',
    unityImportNote: 'Sprite (2D and UI)',
  },
  {
    id: 'violet_potion',
    name: 'Высокое Зелье Опыта (+100 XP)',
    category: 'potions',
    rarityColor: '#A855F7',
    iconEmoji: '🧪',
    promptRu: 'Иконка зелья, фиолетовая алхимическая колба с рунами, светящийся аметистовый эликсир, прозрачный фон PNG',
    promptEn: '2D game icon, high experience potion in violet glass vial with arcane glowing runes, isolated png, transparent background --no background',
    unityImportNote: 'Sprite (2D and UI)',
  },
  {
    id: 'blue_potion',
    name: 'Среднее Зелье Опыта (+50 XP)',
    category: 'potions',
    rarityColor: '#3B82F6',
    iconEmoji: '⚗️',
    promptRu: 'Иконка зелья, синяя сапфировая колба с маной и пузырьками, прозрачный фон PNG',
    promptEn: '2D game icon, mana experience potion in cyan blue sapphire vial with sparkling bubbles, isolated png, transparent background --no background',
    unityImportNote: 'Sprite (2D and UI)',
  },
  {
    id: 'horizontal_scale',
    name: 'Горизонтальная шкала поклевки (Horizontal Frame)',
    category: 'scales',
    rarityColor: '#D946EF',
    iconEmoji: '📏',
    promptRu: '2D интерфейс фэнтези игры, длинная горизонтальная рамка шкалы, кристалл и золотая оправа с фиолетовыми рунами, прозрачный центр для шкалы, прозрачный фон PNG',
    promptEn: '2D mobile game UI, horizontal long sleek hollow gauge frame, ornate dark metal and violet rune crystal trim, golden indicators at center, isolated png, transparent background --no background',
    unityImportNote: 'Image Type: Sliced / 9-Slice Sprite',
  },
  {
    id: 'laser_runner',
    name: 'Внутренний лазерный луч / бегунок шкалы (2 расходящихся луча)',
    category: 'scales',
    rarityColor: '#EC4899',
    iconEmoji: '⚡',
    promptRu: '2D интерфейс игры, вертикальный светящийся лазерный луч-бегунок, неоновое фиолетово-розовое свечение, тонкая полоса индикатора, прозрачный фон PNG',
    promptEn: '2D game UI, vertical glowing laser slider indicator bar, glowing neon magenta and violet crystal line with energy aura, isolated png, transparent background --no background',
    unityImportNote: 'Sprite (2D and UI)',
  },
  {
    id: 'fishing_rod',
    name: 'Волшебная алхимическая удочка (FishRod_Visual)',
    category: 'environment',
    rarityColor: '#F59E0B',
    iconEmoji: '🎣',
    promptRu: '2D игровой ассет, волшебная алхимическая удочка из резного полированного дерева с золотыми светящимися рунами, катушка из латуни со светящейся леской, рубиновый наконечник, прозрачный фон PNG',
    promptEn: '2D mobile game asset, magical fantasy alchemy fishing rod, ancient carved polished wood with glowing golden runes, brass reel with glowing cyan thread, ruby gemstone tip, elegant curved shape, isolated png, transparent background, high resolution --no background',
    unityImportNote: 'Pivot: Bottom-Left (0, 0) для удобного покачивания замаха',
  },
];

export const AssetGallery: React.FC = () => {
  const [filter, setFilter] = useState<'all' | 'locations' | 'potions' | 'scales' | 'environment'>('all');
  const [copiedId, setCopiedId] = useState<string | null>(null);

  const copyText = (text: string, id: string) => {
    navigator.clipboard.writeText(text);
    setCopiedId(id);
    setTimeout(() => setCopiedId(null), 2000);
  };

  const filtered = ASSETS_PROMPTS.filter(a => filter === 'all' || a.category === filter);


  return (
    <div className="flex flex-col gap-6 max-w-6xl mx-auto">
      {/* Header */}
      <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-6 flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-amber-500/20 border border-amber-500/40 flex items-center justify-center text-amber-400">
            <ImageIcon className="w-5 h-5" />
          </div>
          <div>
            <h2 className="text-xl font-bold text-white">ЧАСТЬ 1. Промпты для генерации графики (нейросети)</h2>
            <p className="text-xs text-slate-400">
              Готовые промпты на русском и английском для генерации зелий, шкал и удочки без фона (PNG)
            </p>
          </div>
        </div>

        {/* Filter Pills */}
        <div className="flex items-center gap-2 bg-slate-950 p-1.5 rounded-xl border border-slate-800 flex-wrap">
          {(['all', 'locations', 'potions', 'scales', 'environment'] as const).map(cat => (
            <button
              key={cat}
              onClick={() => setFilter(cat)}
              className={`px-3 py-1.5 rounded-lg text-xs font-bold transition capitalize ${
                filter === cat ? 'bg-amber-500 text-slate-950 shadow' : 'text-slate-400 hover:text-white'
              }`}
            >
              {cat === 'all'
                ? 'Все ассеты'
                : cat === 'locations'
                ? '3 Локации Поиска (8K)'
                : cat === 'potions'
                ? 'Зелья (10 шт)'
                : cat === 'scales'
                ? 'Шкалы (2 шт)'
                : 'Удочка и пруд'}
            </button>
          ))}
        </div>
      </div>

      {/* Grid of Prompts */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {filtered.map(asset => (
          <div
            key={asset.id}
            className="bg-slate-900/80 border border-slate-800 rounded-2xl p-5 flex flex-col justify-between gap-4 hover:border-slate-700 transition"
          >
            <div>
              <div className="flex items-center justify-between mb-2">
                <div className="flex items-center gap-2.5">
                  <span className="text-2xl">{asset.iconEmoji}</span>
                  <h3 className="text-sm font-bold text-white" style={{ color: asset.rarityColor }}>
                    {asset.name}
                  </h3>
                </div>
                <span className="text-[10px] px-2 py-0.5 rounded-full bg-slate-950 border border-slate-800 text-slate-400">
                  {asset.category}
                </span>
              </div>

              {/* Russian Prompt */}
              <div className="bg-slate-950 rounded-xl p-3 border border-slate-800/80 mb-2">
                <div className="flex items-center justify-between mb-1">
                  <span className="text-[10px] uppercase font-bold text-amber-400">Промпт для Алисы / Kandinsky (RU):</span>
                  <button
                    onClick={() => copyText(asset.promptRu, `${asset.id}_ru`)}
                    className="text-[10px] text-slate-400 hover:text-amber-300 flex items-center gap-1 font-semibold"
                  >
                    {copiedId === `${asset.id}_ru` ? <Check className="w-3 h-3 text-emerald-400" /> : <Copy className="w-3 h-3" />}
                    <span>{copiedId === `${asset.id}_ru` ? 'Скопировано' : 'Копировать'}</span>
                  </button>
                </div>
                <p className="text-xs text-slate-300 leading-relaxed font-sans">{asset.promptRu}</p>
              </div>

              {/* English Prompt */}
              <div className="bg-slate-950 rounded-xl p-3 border border-slate-800/80">
                <div className="flex items-center justify-between mb-1">
                  <span className="text-[10px] uppercase font-bold text-purple-400">Промпт для Midjourney / DALL-E (EN):</span>
                  <button
                    onClick={() => copyText(asset.promptEn, `${asset.id}_en`)}
                    className="text-[10px] text-slate-400 hover:text-purple-300 flex items-center gap-1 font-semibold"
                  >
                    {copiedId === `${asset.id}_en` ? <Check className="w-3 h-3 text-emerald-400" /> : <Copy className="w-3 h-3" />}
                    <span>{copiedId === `${asset.id}_en` ? 'Скопировано' : 'Копировать'}</span>
                  </button>
                </div>
                <p className="text-xs text-slate-400 leading-relaxed font-mono">{asset.promptEn}</p>
              </div>
            </div>

            {/* Unity Setup Note */}
            <div className="flex items-center gap-2 text-[11px] text-emerald-400 bg-emerald-950/40 px-3 py-1.5 rounded-lg border border-emerald-500/20">
              <Layers className="w-3.5 h-3.5 shrink-0" />
              <span>{asset.unityImportNote}</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
