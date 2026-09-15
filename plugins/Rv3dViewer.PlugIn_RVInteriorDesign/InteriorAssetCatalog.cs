namespace Rv3dViewer.RVInteriorDesignPlugin;

internal static class InteriorAssetCatalog
{
    public static IReadOnlyList<string> Categories { get; } =
    [
        "家具", "電器", "燈具", "門窗", "廚房", "衛浴",
        "收納", "軟裝", "植栽", "其他", "使用者自訂"
    ];

    public static IReadOnlyList<InteriorAssetDescriptor> CreateDefault() =>
    [
        Asset("單人沙發", "家具", "沙發", 90, 85, 82, favorite: true),
        Asset("三人沙發", "家具", "沙發", 210, 90, 82),
        Asset("餐桌", "家具", "桌", 160, 85, 75),
        Asset("餐椅", "家具", "椅", 48, 52, 86),
        Asset("雙人床", "家具", "床", 188, 210, 95),
        Asset("床頭櫃", "家具", "櫃", 45, 42, 52),

        Asset("壁掛電視", "電器", "影音", 123, 8, 72),
        Asset("直立式冰箱", "電器", "廚房家電", 75, 75, 185),
        Asset("分離式冷氣", "電器", "空調", 90, 25, 32),
        Asset("洗衣機", "電器", "清潔家電", 60, 65, 100),

        Asset("吸頂燈", "燈具", "主燈", 48, 48, 12, favorite: true),
        Asset("吊燈", "燈具", "主燈", 50, 50, 90),
        Asset("嵌燈", "燈具", "基礎照明", 10, 10, 8),
        Asset("軌道燈", "燈具", "重點照明", 8, 16, 18),
        Asset("檯燈", "燈具", "局部照明", 30, 30, 48),
        Asset("立燈", "燈具", "局部照明", 38, 38, 165),

        Asset("單開門", "門窗", "門", 90, 12, 210),
        Asset("雙開門", "門窗", "門", 180, 12, 210),
        Asset("橫拉窗", "門窗", "窗", 180, 12, 120),
        Asset("落地窗", "門窗", "窗", 240, 15, 240),

        Asset("下櫃模組", "廚房", "櫥櫃", 60, 60, 85),
        Asset("吊櫃模組", "廚房", "櫥櫃", 60, 35, 75),
        Asset("廚房中島", "廚房", "工作檯", 180, 90, 90),
        Asset("水槽", "廚房", "設備", 75, 48, 20),

        Asset("洗手台", "衛浴", "面盆", 80, 55, 85),
        Asset("馬桶", "衛浴", "衛生設備", 38, 70, 78),
        Asset("浴缸", "衛浴", "沐浴設備", 170, 75, 58),
        Asset("淋浴拉門", "衛浴", "隔間", 100, 6, 200),

        Asset("開放層架", "收納", "層架", 90, 35, 180),
        Asset("衣櫃", "收納", "櫃體", 180, 60, 240),
        Asset("電視櫃", "收納", "矮櫃", 180, 45, 55),

        Asset("窗簾", "軟裝", "窗飾", 240, 8, 260),
        Asset("地毯", "軟裝", "織品", 200, 300, 1),
        Asset("抱枕", "軟裝", "織品", 45, 15, 45),
        Asset("掛畫", "軟裝", "裝飾", 80, 4, 60),

        Asset("大型盆栽", "植栽", "室內植栽", 65, 65, 170),
        Asset("桌上盆栽", "植栽", "室內植栽", 25, 25, 38),
        Asset("植生牆模組", "植栽", "綠牆", 100, 18, 100),

        Asset("樓梯", "其他", "建築配件", 100, 300, 280),
        Asset("欄杆", "其他", "建築配件", 120, 8, 100),
        Asset("人物比例尺", "其他", "參考", 45, 30, 175),

        Asset("尚未加入自訂模型", "使用者自訂", "本機資產", 0, 0, 0, source: "本機", license: "由使用者確認")
    ];

    private static InteriorAssetDescriptor Asset(
        string name,
        string category,
        string subcategory,
        decimal width,
        decimal depth,
        decimal height,
        string source = "內建目錄",
        string license = "CC0／公有領域候選（下載前須複核）",
        bool favorite = false) =>
        new()
        {
            Name = name,
            Category = category,
            Subcategory = subcategory,
            Source = source,
            License = license,
            Width = width,
            Depth = depth,
            Height = height,
            QualityStatus = "參考項目",
            QualityMessage = "尚未綁定模型檔案；可使用線上模型搜尋功能尋找替代模型。",
            IsFavorite = favorite
        };
}
