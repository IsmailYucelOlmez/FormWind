# FormWind

WinForms kontrolleri için Tailwind benzeri utility-class stil sistemi. Forma bir `TailwindProvider` bırakın; her kontrolün property penceresinde `Classes` alanı belirir:

```
btnSave  →  Classes: "bg-blue-600 text-white px-4 py-2 rounded-md font-bold hover:bg-blue-700"
```

- Hedefler: .NET Framework 4.7.2+ ve .NET 8+ (`net472`, `net8.0-windows`)
- Çekirdek paketin .NET dışında bağımlılığı yok (`net472` için yalnızca `System.Text.Json`)
- Durum varyantları: `hover:`, `focus:`, `active:`, `disabled:`, `checked:`, `dark:` ve birleşimleri (`dark:hover:`)
- Flex ve grid yerleşimi (`flex`, `gap-4`, `justify-between`, `flex-1`, `w-1/2`, `grid-cols-3`, `col-span-2`)
- Bileşen class'ları (`btn-primary`, `card`) ve JSON tema dosyası
- Yuvarlak köşeleri antialias çizen `TwButton`, `TwPanel`, `TwTextBox`
- İsteğe bağlı Krypton Toolkit entegrasyonu (`FormWind.Krypton`)

| Açık tema | Koyu tema |
| --- | --- |
| ![Açık](docs/showcase-light.png) | ![Koyu](docs/showcase-dark.png) |

## Hızlı başlangıç

1. Paketi ekleyin (bkz. [Paketleme](#paketleme)) ya da `src/FormWind` projesine proje referansı verin.
2. `Program.Main` içinde, formlar açılmadan önce temayı yükleyin (dosya yoksa varsayılan tema kullanılır):

   ```csharp
   TwTheme.Current = TwTheme.LoadIfExists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tw.config.json"));
   ```

3. Toolbox'tan `TailwindProvider`'ı forma sürükleyin. Görünmüyorsa Toolbox → sağ tık → **Choose Items** → `FormWind.dll`.
4. Kontrolleri seçip `Classes on tailwindProvider1` alanını doldurun. Designer şunu üretir:

   ```csharp
   this.tailwindProvider1.SetClasses(this.btnSave, "btn-primary");
   ```

5. Koyu tema: `tailwindProvider1.DarkMode = true;`

## Class sözlüğü

| Kategori | Kalıp | Örnek | WinForms karşılığı |
| --- | --- | --- | --- |
| Arka plan | `bg-{renk}-{ton}`, `bg-[#hex]` | `bg-blue-600`, `bg-white` | `BackColor` |
| Yazı rengi | `text-{renk}-{ton}` | `text-gray-900` | `ForeColor` |
| Yazı boyutu | `text-xs … text-4xl` | `text-sm` = 14 px | `Font` (px × 0,75 = pt) |
| Yazı stili | `font-thin` … `font-black`, `font-sans`, `font-mono`, `italic`, `underline` | `font-semibold` | `Font` (bkz. [Font kalınlıkları](#font-kalınlıkları)) |
| İç boşluk | `p`, `px`, `py`, `pt`, `pr`, `pb`, `pl` + `-{n}` / `-px` / `-[12px]` | `px-4` = 16 px | `Padding` |
| Dış boşluk | `m`, `mx`, `my`, `mt`, `mr`, `mb`, `ml` + `-{n}` | `mt-2` = 8 px | `Margin` (layout panellerinde) |
| Boyut | `w-{n}`, `h-{n}` | `h-10` = 40 px | `Width`, `Height` |
| Kenarlık | `border`, `border-{0,2,4,8}`, `border-{renk}-{ton}` | `border border-gray-300` | Çizim |
| Köşe | `rounded`, `rounded-{none,sm,md,lg,xl,2xl,3xl,full}`, `rounded-[10px]` | `rounded-md` = 6 px | `Region` veya owner-draw |
| Hizalama | `text-left`, `text-center`, `text-right` | | `TextAlign` |
| Diğer | `cursor-pointer`, `cursor-text`, `hidden`, `visible`, `truncate` | | `Cursor`, `Visible`, `AutoEllipsis` |

- Boşluk birimi 4 px'tir (`spacingUnit` ile değiştirilebilir). Sayılar her kültürde nokta ile yazılır: `p-0.5`.
- Palet: Tailwind v3'ün 22 rengi × 11 tonu (50–950), ayrıca `white`, `black`, `transparent`.
- Aynı varyant içinde sonra gelen class kazanır: `p-2 px-4` → dikey 8 px, yatay 16 px.
- Tanınmayan class'lar `TailwindProvider.UnknownClasses` olayına (dinleyici yoksa Debug çıktısına) bildirilir.

## Yerleşim (flex ve grid)

`flex` ya da `grid` class'ı verilen bir **`TwPanel`** çocuklarını kendisi yerleştirir; çocukların `Location`'ı, `Dock` ve `Anchor` değerleri yok sayılır. Standart `Panel` ve `Form`'un yerleşim motoru değiştirilemediğinden flex/grid yalnızca `TwPanel` (ya da `ITwLayoutHost` uygulayan bir konteyner) üzerinde çalışır.

```csharp
// Kart: dikey dizilim, öğeler genişliği doldurur
this.tailwindProvider1.SetClasses(this.pnlCard, "card flex flex-col gap-1");
// Buton satırı: sağa yaslı, 8 px aralıklı
this.tailwindProvider1.SetClasses(this.pnlActions, "flex justify-end gap-2");
// Boşluk doldurucu: kalan alanı kaplar ve buton satırını alta iter
this.tailwindProvider1.SetClasses(this.pnlSpacer, "flex-1");
```

| Konteyner (TwPanel) | Öğe (çocuk kontrol) |
| --- | --- |
| `flex`, `grid`, `block` | `flex-1`, `flex-auto`, `flex-initial`, `flex-none` |
| `flex-row`, `flex-col`, `flex-wrap`, `flex-nowrap` | `grow`, `grow-0`, `shrink`, `shrink-0` |
| `gap-{n}`, `gap-x-{n}`, `gap-y-{n}` | `basis-{n}`, `basis-1/2`, `basis-full`, `basis-auto` |
| `justify-start/end/center/between/around/evenly` | `w-1/2`, `w-full`, `h-full` (oranlar konteynere göre) |
| `items-start/end/center/stretch` | `min-w-{n}`, `max-w-{n}`, `max-w-md` …, `min-h-{n}`, `max-h-{n}` |
| `grid-cols-{1–12}` | `self-start/end/center/stretch`, `col-span-{1–12}`, `col-span-full`, `m-*` |

CSS'ten farklar:
- Öğelerin "doğal" boyutu Designer'daki `Size`'dır (AutoSize kontrollerde tercih edilen boyut). Boyutu belirtilmemiş öğeler doğal boyutlarının altına küçülmez.
- Öğe aralığı yalnızca `m-*` class'larından gelir; WinForms'un varsayılan 3 px `Margin`'i kullanılmaz.
- Varsayılan `items-stretch`: satırdaki öğeler konteyner yüksekliğine, sütundaki öğeler genişliğine uzar.
- Grid satır yükseklikleri içerikten belirlenir; `grid-rows-*`, `row-span-*`, `margin: auto` ve `order-*` yok.
- Öğeler satırın çapraz boyutunu aşmaz (CSS'te taşabilir; WinForms'ta taşan kısım kesilip kaybolurdu). Daralan Label'larda `truncate` "…" gösterir.
- `TwPanel`'in içeriği kenarlığın içinde başlar (CSS kutu modeli); `AutoScroll` panelde genişlik görünen alandan alınır ve dikey kaydırma çubuğunun yeri önceden ayrılır.
- `AutoSize = true` olan flex `TwPanel` içeriğine göre boyutlanır (grid için henüz yok).
- Responsive varyantlar (`sm:`, `md:`) henüz yok.

## Font kalınlıkları

`font-thin` (100), `font-extralight` (200), `font-light` (300), `font-normal` (400), `font-medium` (500), `font-semibold` (600), `font-bold` (700), `font-extrabold` (800), `font-black` (900).

WinForms yalnızca normal ve kalın stilleri bilir; ara kalınlıklar Windows'ta **ayrı font aileleri** olarak kuruludur. FormWind kalınlığı bu ailelere CSS'in eşleme kuralıyla çözer:

| Class | Segoe UI'da kullanılan |
| --- | --- |
| `font-thin`, `font-extralight`, `font-light` | Segoe UI Light |
| `font-normal`, `font-medium` | Segoe UI |
| `font-semibold` | Segoe UI Semibold |
| `font-bold` | Segoe UI + kalın stil |
| `font-extrabold`, `font-black` | Segoe UI Black |

Başka bir font için ağırlık aileleri tema dosyasında tanımlanır; kurulu olmayan aile atlanır ve en yakın kurulu kalınlık seçilir:

```jsonc
"fontWeights": { "Inter": { "300": "Inter Light", "600": "Inter SemiBold" } }
```

## İkonlar

Windows'la gelen ikon fontu kullanılır: Windows 11'de **Segoe Fluent Icons**, Windows 10'da **Segoe MDL2 Assets** (Windows 7/8'de ikon çizilmez).

- **`TwIcon`**: tek başına ikon. `IconName` property'si Designer'da açılır listeden seçilir. Renk `text-*`, boyut font boyutuyla verilir: `text-xl` = 20×20 px. AutoSize varsayılan olarak açıktır.
- **`TwButton.IconName`**: metnin yanında ikon. `IconPosition` (Left/Right) ve `IconSpacing` (varsayılan 8 px) ayarlanabilir.

```csharp
this.icoCustomer.IconName = "user";
this.tailwindProvider1.SetClasses(this.icoCustomer, "text-xl text-blue-600 dark:text-blue-400");
this.btnSave.IconName = "save";
```

Yerleşik adlar: `arrow-down`, `arrow-left`, `arrow-right`, `arrow-up`, `attach`, `bell`, `calculator`, `calendar`, `camera`, `cart`, `check`, `chevron-down`, `chevron-left`, `chevron-right`, `chevron-up`, `clock`, `copy`, `cut`, `download`, `edit`, `error`, `eye`, `eye-off`, `file`, `filter`, `flag`, `folder`, `folder-open`, `fullscreen`, `globe`, `grid`, `heart`, `heart-filled`, `help`, `history`, `home`, `image`, `info`, `key`, `link`, `list`, `location`, `lock`, `mail`, `map-pin`, `menu`, `minus`, `more`, `paste`, `phone`, `pin`, `plus`, `power`, `print`, `redo`, `refresh`, `save`, `search`, `send`, `settings`, `share`, `shield`, `sort`, `star`, `star-filled`, `sync`, `tag`, `trash`, `undo`, `unlock`, `upload`, `user`, `users`, `warning`, `x`, `zoom-in`, `zoom-out`.

Fontun herhangi bir glyph'i kod noktasıyla da kullanılabilir (`IconName = "#E8A5"`). Ek adlar ve farklı bir ikon fontu tema dosyasından verilir:

```jsonc
"iconFont": "Segoe Fluent Icons",
"icons": { "invoice": "E8A5" }
```

Glyph'ler font metriklerine güvenilmeden, gerçek mürekkep sınırlarına göre ortalanarak çizilir (ikon fontlarının satır yüksekliği glyph boyutuyla uyuşmaz).

## Kenarlık, gölge ve focus halkası

| Class | Etki |
| --- | --- |
| `border-t`, `border-b-2`, `border-x`, `border-y` | Kenar bazlı kenarlık (köşesiz çizilir) |
| `shadow-sm`, `shadow`, `shadow-md`, `shadow-lg`, `shadow-xl`, `shadow-2xl`, `shadow-none` | Tailwind v3 gölge katmanları (bulanık) |
| `shadow-{renk}`, `shadow-blue-500/50` | Renkli gölge |
| `ring`, `ring-0/1/2/4/8`, `ring-{renk}`, `ring-offset-2`, `ring-offset-{renk}` | Halka; genelde `focus:ring-2 focus:ring-blue-500/30` |
| `bg-black/50`, `text-white/75` … | Saydamlık: tüm renk class'larında `/0–100` |

Gölge ve halka kontrolün **ebeveyninde** çizilir (kontroller kendi sınırlarının dışına çizemez); Form, Panel ve TwPanel ebeveynlerinde çalışır. Bu sayede `TextBox` gibi native kontrollerin de focus halkası olabilir.

## Gradient

`bg-gradient-to-{r,l,t,b,tr,tl,br,bl} from-{renk} via-{renk} to-{renk}` — `TwPanel`, `TwButton` ve standart `Panel`'de çizilir. Eksik uç diğer ucun saydam halidir.

## Etkileşim

- **`transition`** (`transition-colors`, `duration-75` … `duration-1000`, `duration-[250ms]`): arka plan ve metin rengi değişimleri yumuşak geçişle (ease-out, varsayılan 150 ms) uygulanır.
- **`group` / `group-hover:`**: `group` class'lı konteynerin üzerine gelindiğinde içindeki kontrollerin `group-hover:` varyantları uygulanır. Hover'a tepki veren konteynerler, imleç doğrudan bir çocuk kontrolün üzerinden girse de hover sayılır.
- **Placeholder**: `TwTextBox.PlaceholderText`; renk `placeholder:text-gray-400`.

## CheckBox, Radio, Switch

`TwCheckBox`, `TwRadioButton`, `TwSwitch` owner-draw çizilir. Seçili renk `accent-*`, seçili değilken dolgu `fill-*`, kutu kenarlığı `border-*`, köşe `rounded-*`, metin `text-*`. `TwSwitch` (44×24 px) düğmesi 150 ms içinde kayar.

## Menü ve araç çubukları

MenuStrip, ToolStrip, StatusStrip ve ContextMenuStrip için tema renderer'ı. Temel class'lar şeride, `menu:` açılan menülere, `item:` öğelere uygulanır:

```csharp
SetClasses(menuStrip1, "bg-white text-gray-700 border-b menu:bg-white menu:border-gray-200 item:hover:bg-gray-100 item:rounded-md divide-gray-200 dark:bg-gray-900 dark:text-gray-200");
```

## Başlık çubuğu

Form'a class verildiğinde koyu tema açıkken başlık çubuğu otomatik koyulaşır (Windows 10 20H1+). Windows 11'de `caption:bg-*`, `caption:text-*`, `caption:border-*` ile renklendirilir.

## DataGridView

DataGridView'un bölümleri **parça önekleriyle** stillenir; durum önekleriyle birleştirilebilir (`dark:header:bg-gray-900`, `row:hover:bg-gray-50`). Hazır `table` bileşeni tek class'la açık ve koyu temalı bir tablo verir:

```csharp
this.tailwindProvider1.SetClasses(this.gridOrders, "table");
```

| Önek | Hedef | Desteklenen class'lar |
| --- | --- | --- |
| (yok) | Kontrol alanı ve tüm hücreler | `bg-*`, `text-*`, font, `border`, `divide-x`, `divide-y`, `divide-{renk}` (ızgara çizgileri) |
| `cell:` | `DefaultCellStyle` | renk, font, `px-*`/`py-*`, `text-left/center/right`, `whitespace-nowrap` |
| `alt:` | `AlternatingRowsDefaultCellStyle` | renk, font |
| `selected:` | Seçim renkleri | `bg-*`, `text-*` |
| `header:` | Kolon başlıkları | renk, font, padding, hizalama, `h-*`, `border-0`, `whitespace-nowrap` |
| `row:` | Satırlar | `h-*` (satır yüksekliği), `row:hover:bg-*` / `row:hover:text-*` (satır hover'ı) |
| `row-header:` | Satır başlıkları | `hidden`, renk |

- Başlık renkleri verildiğinde `EnableHeadersVisualStyles` kapatılır (Windows teması başlık renklerini yok sayar).
- Class'lar kaldırıldığında grid'in orijinal stillerine dönülür.
- Kolon bazlı stil (örn. yalnızca tutar kolonunu sağa yaslamak) class'larla değil, kolonun `DefaultCellStyle`'ıyla yapılır.
- Kaydırma çubukları native çizilir ve koyu temada açık renkte kalır.
- Parça önekleri DataGridView dışındaki kontrollerde yok sayılır.

### Varyantların uygulanma sırası

Sonraki, öncekini ezer: temel → `dark:` → `checked:` → `focus:` → `hover:` → `dark:hover:` → `active:` → `disabled:`.
Kontrol devre dışıyken `focus`, `hover` ve `active` uygulanmaz.

## Tema dosyası (`tw.config.json`)

```jsonc
{
  "fontFamily": "Segoe UI",
  "spacingUnit": 4,
  "colors": {
    "brand": { "500": "#2b579a", "600": "#234a85" },   // bg-brand-500
    "surface": "#f3f6fb"                                  // bg-surface
  },
  "components": {
    "btn-primary": "btn bg-brand-500 text-white hover:bg-brand-600"
  }
}
```

Dosya varsayılan palet ve bileşenlerin üzerine eklenir. Varsayılan bileşenler: `btn`, `btn-primary`, `btn-ghost`, `btn-danger`, `card`, `input`, `table` (bkz. `TwTheme.DefaultComponents`). Designer tema dosyasını okumadığından tasarım zamanı önizlemesi varsayılanlarla yapılır.

## Örnek: Rickipedia

`samples/Rickipedia`, [The Rick and Morty API](https://rickandmortyapi.com) verisiyle bir karakter listesi sayfasıdır: ada göre arama, durum/tür/cinsiyet filtreleri, satır sayısı seçimi, koyu tema ve satıra tıklayınca ayrıntı penceresi. Sayfanın tamamı FormWind class'larıyla stillenmiş standart WinForms kontrolleri ve `Tw*` kontrolleridir; sayfa bileşenleri `tw.config.json` içindedir.

| Açık tema | Koyu tema |
| --- | --- |
| ![Rickipedia açık](docs/rickipedia-light.png) | ![Rickipedia koyu](docs/rickipedia-dark.png) |

```bash
samples/Rickipedia/bin/Debug/net472/Rickipedia.exe --snapshot sayfa.png --dark --menu --details
```

## Kontroller

| Kontrol | Ne zaman |
| --- | --- |
| `TwPictureBox` | Pürüzsüz yuvarlak köşeli resim (`rounded-lg`, `rounded-full` avatar); `object-cover` / `object-contain` / `object-fill`. |
| `TwSelect` | Stillenebilir açılır liste (native ComboBox'ın yerine): kutu temel class'larla, menü `menu:` / `item:` ile. Genişlik varsayılan olarak seçili öğeye göredir (`SizeToSelection`). |
| `TwIcon` | Tek başına ikon (bkz. [İkonlar](#i̇konlar)). |
| `TwCheckBox`, `TwRadioButton`, `TwSwitch` | Bkz. [CheckBox, Radio, Switch](#checkbox-radio-switch). |
| Standart kontroller | Renk, yazı ve boşluk her kontrolde çalışır. Köşe `Region` ile kırpılır (antialias yok), kenarlık `Paint` olayında çizilir. Native kontrollerde (`TextBox`, `ComboBox`, `ListBox` …) köşe ve kenarlık uygulanmaz. |
| `TwButton` | Pürüzsüz köşeli buton. `Image` henüz çizilmez, `Paint` olayı tetiklenmez. |
| `TwPanel` | Pürüzsüz köşeli, kenarlıklı panel (kart). |
| `TwTextBox` | Kenarlıksız TextBox'ı yuvarlak bir panel içine alır; `input` stili için. |

## Krypton Toolkit

```csharp
FormWind.Krypton.KryptonApplier.Register();   // Program.Main içinde
```

`KryptonButton`, `KryptonPanel`, `KryptonTextBox` ve `KryptonLabel` için varyantlar Krypton'un durum paletlerine eşlenir: temel → `StateCommon`, `hover:` → `StateTracking`, `active:` → `StatePressed`, `focus:` → `OverrideFocus`, `disabled:` → `StateDisabled`.

## DPI

Piksel değerleri kontrolün `DeviceDpi / 96` oranıyla ölçeklenir. Uygulama DPI-aware (örn. `PerMonitorV2`) çalışıyor ama formların AutoScale'i yerleşimi büyütmüyorsa boşluklar büyür, kontroller büyümez ve metinler sığmaz. Bu durumda AutoScale'i düzeltin ya da `TailwindProvider.ScaleWithDpi = false` yapın. DPI-unaware uygulamalarda (çoğu 4.7.2 projesinin varsayılanı) Windows ölçeklemeyi kendisi yapar ve ek ayar gerekmez.

## Proje yapısı

```
src/FormWind/             Çekirdek: parser, tema, durum motoru, WinFormsApplier, Tw* kontrolleri, Designer editörü
src/FormWind.Krypton/     KryptonApplier
tests/FormWind.Tests/     xUnit testleri (net472 + net8.0-windows)
samples/FormWind.Showcase/ Vitrin uygulaması (net472)
```

## Geliştirme

```bash
dotnet build FormWind.sln
```

```bash
dotnet test FormWind.sln
```

Performans ve GDI testleri (`Category=Performance`) varsayılan çalıştırmada atlanır (Skipped). Toplam ~1 dakika sürerler ve süreç genelindeki GDI sayaçlarını ölçtükleri için diğer testlerle paralel çalışmazlar. Çalıştırmak ve ölçümleri görmek için `FORMWIND_PERF=1` ortam değişkenini ayarlayın (PowerShell):

```powershell
$env:FORMWIND_PERF = "1"; dotnet test FormWind.sln --filter Category=Performance --logger "console;verbosity=detailed"
```

Visual Studio Test Explorer'da çalıştırmak için değişkeni Visual Studio'yu açmadan önce sistemde tanımlayın.

Vitrin formunun ekran görüntüsü (görsel karşılaştırma için):

```bash
samples/FormWind.Showcase/bin/Debug/net472/FormWind.Showcase.exe --snapshot light.png
```

## Paketleme

```bash
dotnet pack FormWind.sln -c Release
```

Paketler `artifacts/packages` klasörüne yazılır (`FormWind`, `FormWind.Krypton` ve `.snupkg` sembol paketleri). Yerel kullanım için bu klasörü Visual Studio'da Package Sources'a ekleyin.

## Bilinen sınırlamalar ve yapılacaklar

- [ ] Designer, önizlemede uygulanan `BackColor`/`Font` değerlerini `Designer.cs`'e de yazabilir. Çalışma zamanında `SetClasses` bunları ezer; dosyayı temiz tutmak için `DesignTimePreview = false` kullanılabilir.
- [ ] Smart tag (DesignerActionList) ile hızlı bileşen seçimi
- [ ] Mevcut formlardaki sabit renk ve font atamalarını class'a çeviren dönüştürme aracı
- [x] Flex ve grid yerleşimi (`TwPanel`)
- [x] DataGridView uygulayıcısı ve `table` bileşeni
- [x] Font kalınlıkları (`font-thin` … `font-black`) ve ikonlar (`TwIcon`, `TwButton.IconName`)
- [x] Gölge, kenar bazlı kenarlık, focus halkası, renk saydamlığı
- [x] `TwCheckBox`, `TwRadioButton`, `TwSwitch`; menü renderer'ı; başlık çubuğu
- [x] Geçişler, gradient, `group-hover`, placeholder
- [ ] Uygulamayla dağıtılan özel fontlar (Inter vb.) ve SVG ikonlar
- [ ] Responsive varyantlar (`sm:`, `md:`, `lg:`) ve grid için `row-span`, `grid-rows`
- [ ] Gölge ve geçiş animasyonları
- [ ] .NET 8+ out-of-process Designer için `ClassesEditor`'ün Designer SDK ile taşınması
- [ ] Lisans seçimi (paket yayımlanmadan önce)
