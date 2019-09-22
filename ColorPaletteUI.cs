using Terraria.UI;
using Terraria.ModLoader;
using Terraria.GameContent.UI.Elements;
using Terraria;
using Terraria.ID;
using Terraria.Map;
using Microsoft.Xna.Framework;
using System.Reflection;
using Terraria.ModLoader.IO;
using Terraria.ModLoader.UI.Elements;

namespace ColorPalette
{
    class ColorPaletteUI : UIState
    {
        public static bool Visible { get; set; }

        public UIPanel colorPalettePanel;
        public UITextPanel<string> paletteTitle;
        public UIList colorList;

        internal PaletteIO currentIO;

        public UITextPanel<string> createColorButton;
        private const string CreateButtonDefaultText = "Add selecting color";

        public override void OnInitialize()
        {
            if (currentIO == null)
            {
                if (PaletteIO.palettes.Count >= 1)
                {
                    currentIO = PaletteIO.palettes[0];
                }
                else
                {
                    currentIO = new PaletteIO()
                    {
                        name = "palette",
                        requireSave = true
                    };
                    PaletteIO.palettes.Add(currentIO);
                }
            }

            colorPalettePanel = new UIPanel()
            {
                Top = new StyleDimension(Main.screenHeight / 2, 0f),
                Left = new StyleDimension(Main.screenWidth - 500, 0f),
                Width = new StyleDimension(300, 0f),
                Height = new StyleDimension(500f, 0f)
            };
            Append(colorPalettePanel);

            paletteTitle = new UITextPanel<string>("Color Palette")
            {
                Top = new StyleDimension(-30, 0f),
                Left = new StyleDimension(20, 0f),
                Width = new StyleDimension(-40, 1f),
                Height = new StyleDimension(40, 0f),
                BackgroundColor = new Color(73, 94, 171)
        };
            colorPalettePanel.Append(paletteTitle);

            var scrollbar = new Terraria.GameContent.UI.Elements.FixedUIScrollbar(ColorPalette.instance.userInterface)
            {
                Height = new StyleDimension(-15f, 1f),
                HAlign = 1f,
                VAlign = 1f
            };
            scrollbar.SetView(100f, 1000f);
            colorPalettePanel.Append(scrollbar);

            colorList = new UIList
            {
                Width = new StyleDimension(-25f, 1f),
                Height = new StyleDimension(-15f, 1f),
                ListPadding = 5f,
                VAlign = 1f
            };
            colorList.SetScrollbar(scrollbar);
            colorPalettePanel.Append(colorList);

            ColorUI.parentUI = this;
            ColorUI.parentList = colorList;

            foreach (var data in currentIO.datas)
            {
                Item item = new Item();
                if (data.id < ItemID.Count)
                {
                    item.SetDefaults(data.id);
                }
                else
                {
                    ModContent.SplitName(data.fullName, out string modName, out string itemName);
                    var modItem = ModLoader.GetMod(modName)?.GetItem(itemName);
                    if (modItem != null)
                    {
                        item = modItem.item;
                    }
                }
                if (item.type != 0)
                {
                    var colorUI = new ColorUI(item, data.paint)
                    {
                        colorData = data
                    };
                    colorList.Add(colorUI);
                }
            }

            createColorButton = new UITextPanel<string>(CreateButtonDefaultText)
            {
                Width = new StyleDimension(0f, 1f),
                Height = new StyleDimension(30f, 0f)
            };
            createColorButton.OnClick += CreateColorButton_OnClick;
            colorList.Add(createColorButton);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }
        }

        private void CreateColorButton_OnClick(UIMouseEvent evt, UIElement listeningElement)
        {
            if (Main.mouseItem == null)
            {
                Main.mouseItem = new Item();
            }
            Item selectedItem = Main.mouseItem.type == 0 ? Main.LocalPlayer.inventory[Main.LocalPlayer.selectedItem] : Main.mouseItem;
            if (selectedItem == null || selectedItem.type == 0 || selectedItem.stack <= 0 || (selectedItem.createTile == -1 && selectedItem.createWall == -1))
            {
                Main.NewText("Please select valid tile/wall.");
                return;
            }
            colorList.Remove(createColorButton);
            byte paintColor = 0;
            if (Main.LocalPlayer.autoPaint && Main.LocalPlayer.builderAccStatus[3] == 0)
            {
                for (int i = 0; i < 50; i++)
                {
                    if (Main.LocalPlayer.inventory[i].paint > 0 && Main.LocalPlayer.inventory[i].stack > 0)
                    {
                        paintColor = Main.LocalPlayer.inventory[i].paint;
                    }
                }
            }
            var colorUI = new ColorUI(selectedItem, paintColor);
            currentIO.datas.Add(colorUI.colorData);
            colorUI.Initialize();
            colorList.Add(colorUI);
            colorList.Add(createColorButton);
        }
    }

    class ColorUI : UIPanel
    {
        internal static UIList parentList;
        internal static ColorPaletteUI parentUI;

        private static FieldInfo colorLookupInfo;

        public UIPanel colorPreview;
        public UITextPanel<string> tileInfoUI;

        public UIImageButton deleteButton;

        public Item PaletteItem { get; private set; }
        public byte PaintColor { get; private set; }

        private readonly Color mapColor;

        internal ColorData colorData;

        public ColorUI(Item item, byte paint = 0)
        {
            PaletteItem = item.Clone();
            PaintColor = paint;
            if (colorLookupInfo == null)
            {
                colorLookupInfo = typeof(MapHelper).GetField("colorLookup", BindingFlags.Static | BindingFlags.NonPublic);
            }
            mapColor = ItemToMapColor(PaletteItem);

            if (PaintColor > 0)
            {
                Color color = WorldGen.paintColor(PaintColor);
                float num = mapColor.R / 255f;
                float num2 = mapColor.G / 255f;
                float num3 = mapColor.B / 255f;
                if (num2 > num)
                {
                    num = num2;
                }
                if (num3 > num)
                {
                    float num4 = num;
                    num = num3;
                    num3 = num4;
                }
                switch (PaintColor)
                {
                    case 29:
                        {
                            float num6 = num3 * 0.3f;
                            mapColor.R = (byte)(color.R * num6);
                            mapColor.G = (byte)(color.G * num6);
                            mapColor.B = (byte)(color.B * num6);
                            break;
                        }
                    case 30:
                        if (PaletteItem.createWall != -1)
                        {
                            mapColor.R = (byte)((float)(255 - mapColor.R) * 0.5f);
                            mapColor.G = (byte)((float)(255 - mapColor.G) * 0.5f);
                            mapColor.B = (byte)((float)(255 - mapColor.B) * 0.5f);
                        }
                        else
                        {
                            mapColor.R = (byte)(255 - mapColor.R);
                            mapColor.G = (byte)(255 - mapColor.G);
                            mapColor.B = (byte)(255 - mapColor.B);
                        }
                        break;
                    default:
                        {
                            float num5 = num;
                            mapColor.R = (byte)((float)(int)color.R * num5);
                            mapColor.G = (byte)((float)(int)color.G * num5);
                            mapColor.B = (byte)((float)(int)color.B * num5);
                            break;
                        }
                }
            }

            string fullName = item.Name;
            if (item.modItem != null)
            {
                fullName = item.modItem.mod.Name + "." + item.modItem.Name;
            }

            colorData = new ColorData()
            {
                id = item.type,
                fullName = fullName,
                paint = PaintColor
            };
        }

        public override void OnInitialize()
        {
            Width = new StyleDimension(0f, 1f);
            Height = new StyleDimension(85f, 0f);

            colorPreview = new UIPanel()
            {
                Width = new StyleDimension(40, 0f),
                Height = new StyleDimension(40, 0f),
                BackgroundColor = mapColor
            };
            Append(colorPreview);

            tileInfoUI = new UITextPanel<string>("", 0.75f)
            {
                Left = new StyleDimension(50, 0f),
                Width = new StyleDimension(150, 0f),
                Height = new StyleDimension(60, 0f)
            };
            if (PaintColor == 0)
            {
                tileInfoUI.SetText(PaletteItem.Name);
            }
            else
            {
                var item = new Item();
                item.SetDefaults(PaintID2ItemID(PaintColor));
                tileInfoUI.SetText(PaletteItem.Name + "\n" + item.Name);
            }
            Append(tileInfoUI);

            deleteButton = new UIImageButton(ModContent.GetTexture("Terraria/UI/ButtonDelete"))
            {
                HAlign = 1f,
                VAlign = 1f
            };
            deleteButton.OnClick += (x, y) =>
            {
                parentList.Remove(this);
                parentUI.currentIO.datas.Remove(colorData);
            };
            Append(deleteButton);
        }

        public override void Click(UIMouseEvent evt)
        {
            int index = -1;
            bool success = true;

            for (int i = 0; i < 50; i++)
            {
                if (Main.LocalPlayer.inventory[i].type == PaletteItem.type)
                {
                    index = i;
                    break;
                }
            }
            if (index == -1)
            {
                success = false;
                Main.NewText(string.Format("You don\'t have {0}.", PaletteItem.Name));
            }

            int paintIndex = -1;
            if (PaintColor == 0)
            {
                Main.LocalPlayer.autoPaint = false;
                Main.LocalPlayer.builderAccStatus[3] = 1;
            }
            else
            {
                for (int i = 0; i < 50; i++)
                {
                    if (Main.LocalPlayer.inventory[i].paint == PaintColor && Main.LocalPlayer.inventory[i].stack > 0)
                    {
                        paintIndex = i;
                        break;
                    }
                }
                if (paintIndex == -1)
                {
                    success = false;
                    var paintItem = new Item();
                    paintItem.SetDefaults(PaintID2ItemID(PaintColor));
                    Main.NewText(string.Format("You don\'t have {0}.", paintItem.Name));
                }
            }

            if (success)
            {
                Utils.Swap(ref Main.LocalPlayer.inventory[index], ref Main.LocalPlayer.inventory[1]);
                if (paintIndex != -1)
                {
                    Utils.Swap(ref Main.LocalPlayer.inventory[paintIndex], ref Main.LocalPlayer.inventory[0]);
                    Main.LocalPlayer.autoPaint = true;
                    Main.LocalPlayer.builderAccStatus[3] = 0;
                }
                Main.LocalPlayer.selectedItem = 1;
            }
            base.Click(evt);
        }

        public static int PaintID2ItemID(byte id)
        {
            switch (id)
            {
                case 1:
                    return ItemID.RedPaint;
                case 2:
                    return ItemID.OrangePaint;
                case 3:
                    return ItemID.YellowPaint;
                case 4:
                    return ItemID.LimePaint;
                case 5:
                    return ItemID.GreenPaint;
                case 6:
                    return ItemID.TealPaint;
                case 7:
                    return ItemID.CyanPaint;
                case 8:
                    return ItemID.SkyBluePaint;
                case 9:
                    return ItemID.BluePaint;
                case 10:
                    return ItemID.PurplePaint;
                case 11:
                    return ItemID.VioletPaint;
                case 12:
                    return ItemID.PinkPaint;
                case 13:
                    return ItemID.DeepRedPaint;
                case 14:
                    return ItemID.DeepOrangePaint;
                case 15:
                    return ItemID.DeepYellowPaint;
                case 16:
                    return ItemID.DeepLimePaint;
                case 17:
                    return ItemID.DeepGreenPaint;
                case 18:
                    return ItemID.DeepTealPaint;
                case 19:
                    return ItemID.DeepCyanPaint;
                case 20:
                    return ItemID.DeepSkyBluePaint;
                case 21:
                    return ItemID.DeepBluePaint;
                case 22:
                    return ItemID.DeepPurplePaint;
                case 23:
                    return ItemID.DeepVioletPaint;
                case 24:
                    return ItemID.DeepPinkPaint;
                case 25:
                    return ItemID.BlackPaint;
                case 26:
                    return ItemID.WhitePaint;
                case 27:
                    return ItemID.GrayPaint;
                case 28:
                    return ItemID.BrownPaint;
                case 29:
                    return ItemID.ShadowPaint;
                case 30:
                    return ItemID.NegativePaint;
                default:
                    return 0;
            }
        }

        public static Color ItemToMapColor(Item item)
        {
            Color[] colorLookup = (Color[])colorLookupInfo.GetValue(null);
            int id = 0;
            if (item.createTile != -1)
            {
                id = MapHelper.tileLookup[item.createTile];
            }
            if (item.createWall != -1)
            {
                id = MapHelper.wallLookup[item.createWall];
            }
            return colorLookup[id];
        }
    }
}