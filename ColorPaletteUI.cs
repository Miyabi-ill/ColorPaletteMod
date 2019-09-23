using Terraria.UI;
using Terraria.ModLoader;
using Terraria.GameContent.UI.Elements;
using Terraria;
using Terraria.ID;
using Terraria.Map;
using Microsoft.Xna.Framework;
using System.Reflection;
using System;
using System.Linq;
using System.Collections;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace ColorPalette
{
    class ColorPaletteUI : UIState
    {
        public static bool Visible { get; set; }

        public UIPanel colorPalettePanel;
        public UITextPanel<string> paletteTitle;
        internal LazyUpdatableGrid colorGrid;

        public UIImageButton createColorButton;
        public UITextPanel<string> deleteColorButton;
        public UIText tileInfoText;

        public ColorPanel selectedPanel = null;

        public override void OnInitialize()
        {
            colorPalettePanel = new UIPanel()
            {
                HAlign = 1f,
                Top = new StyleDimension(85f, 0f),
                Left = new StyleDimension(-40f, 0f),
                Width = new StyleDimension(260f, 0f),
                Height = new StyleDimension(-125f, 1f),
                BackgroundColor = new Color(73, 94, 171)
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

            var scrollbar = new FixedUIScrollbar(ColorPalette.instance.userInterface)
            {
                Top = new StyleDimension(15f, 0f),
                Height = new StyleDimension(-75f, 1f),
                HAlign = 1f
            };
            scrollbar.SetView(100f, 1000f);
            colorPalettePanel.Append(scrollbar);

            colorGrid = new LazyUpdatableGrid()
            {
                ListPadding = 2f,
                Top = new StyleDimension(15f, 0f),
                Width = new StyleDimension(-25f, 1f),
                Height = new StyleDimension(-60f, 1f)
            };
            colorGrid.SetScrollbar(scrollbar);
            colorPalettePanel.Append(colorGrid);

            ColorPanel.parentUI = this;
            ColorPanel.parentGrid = colorGrid;

            deleteColorButton = new UITextPanel<string>("Delete")
            {
                HAlign = 1f,
                VAlign = 1f,
                Width = new StyleDimension(50f, 0f),
                Height = new StyleDimension(30f, 0f)
            };
            deleteColorButton.OnMouseOver += (x, y) => deleteColorButton.BackgroundColor = new Color(73, 94, 171);
            deleteColorButton.OnMouseOut += (x, y) => deleteColorButton.BackgroundColor = new Color(63, 82, 151) * 0.7f;
            deleteColorButton.OnClick += DeleteColorButton_OnClick;
            colorPalettePanel.Append(deleteColorButton);

            tileInfoText = new UIText("No tile selected.")
            {
                VAlign = 1f,
                Height = new StyleDimension(60f, 0f)
            };
            colorPalettePanel.Append(tileInfoText);

            createColorButton = new UIImageButton(ColorPalette.instance.GetTexture("CreateButton"));
            createColorButton.OnClick += CreateColorButton_OnClick;
            colorGrid.Add(createColorButton);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (colorPalettePanel.ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }
            base.DrawSelf(spriteBatch);
        }

        private void DeleteColorButton_OnClick(UIMouseEvent evt, UIElement listeningElement)
        {
            if (selectedPanel != null)
            {
                colorGrid.Remove(selectedPanel);
                tileInfoText.SetText("No tile selected.");
                selectedPanel = null;
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
                Main.NewText("Please select a valid tile/wall.");
                return;
            }
            colorGrid.Remove(createColorButton);
            byte paintColor = 0;
            if (Main.LocalPlayer.autoPaint && Main.LocalPlayer.builderAccStatus[3] == 0)
            {
                for (int i = 0; i < 50; i++)
                {
                    if (Main.LocalPlayer.inventory[i].paint > 0 && Main.LocalPlayer.inventory[i].stack > 0)
                    {
                        paintColor = Main.LocalPlayer.inventory[i].paint;
                        break;
                    }
                }
            }
            var colorUI = new ColorPanel(selectedItem, paintColor);
            colorUI.Activate();
            colorGrid.Add(colorUI);
            colorGrid.Add(createColorButton);
        }

        public List<ColorData> GetDatas()
        {
            List<ColorData> datas = new List<ColorData>();
            foreach (UIElement element in colorGrid._items)
            {
                if (element is ColorPanel panel)
                {
                    datas.Add(panel.colorData);
                }
            }
            return datas;
        }
    }

    class ColorPanel : UIPanel
    {
        internal static LazyUpdatableGrid parentGrid;
        internal static ColorPaletteUI parentUI;

        private static FieldInfo colorLookupInfo;

        private static readonly Type mapLoaderType = typeof(ModTile).Assembly.DefinedTypes.First(x => x.Name == "MapLoader");
        private static FieldInfo wallEntries;
        private static FieldInfo tileEntries;

        public Item PaletteItem { get; private set; }
        public byte PaintColor { get; private set; }

        private readonly Color mapColor;

        internal ColorData colorData;

        private Vector2 offset;
        public bool dragging;

        public ColorPanel(Item item, byte paint = 0)
        {
            PaletteItem = item.Clone();
            PaintColor = paint;
            if (colorLookupInfo == null)
            {
                colorLookupInfo = typeof(MapHelper).GetField("colorLookup", BindingFlags.Static | BindingFlags.NonPublic);
            }
            if (wallEntries == null)
            {
                wallEntries = mapLoaderType.GetField("wallEntries", BindingFlags.Static | BindingFlags.NonPublic);
            }
            if (tileEntries == null)
            {
                tileEntries = mapLoaderType.GetField("tileEntries", BindingFlags.Static | BindingFlags.NonPublic);
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
                            mapColor.R = (byte)((255 - mapColor.R) * 0.5f);
                            mapColor.G = (byte)((255 - mapColor.G) * 0.5f);
                            mapColor.B = (byte)((255 - mapColor.B) * 0.5f);
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
                            mapColor.R = (byte)(color.R * num5);
                            mapColor.G = (byte)(color.G * num5);
                            mapColor.B = (byte)(color.B * num5);
                            break;
                        }
                }
            }
            mapColor.A = 255;

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
            Width = new StyleDimension(30, 0f);
            Height = new StyleDimension(30, 0f);
            BackgroundColor = mapColor;
        }

        public override void MouseDown(UIMouseEvent evt)
        {
            base.MouseDown(evt);
            DragStart(evt);
        }

        public override void MouseUp(UIMouseEvent evt)
        {
            base.MouseUp(evt);
            DragEnd(evt);
        }

        private void DragStart(UIMouseEvent evt)
        {
            offset = new Vector2(evt.MousePosition.X - Left.Pixels, evt.MousePosition.Y - Top.Pixels);
            dragging = true;
            parentGrid.LazyUpdateElement(this);
        }

        private void DragEnd(UIMouseEvent evt)
        {
            Vector2 end = evt.MousePosition;
            dragging = false;

            var element = parentGrid.GetElementAt(end);
            if (element != null && element != this)
            {
                int index = parentGrid.IndexOf(element);
                if (index != -1 && index < parentGrid.Count)
                {
                    parentGrid.Remove(this);
                    parentGrid.Insert(index, this);
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (dragging)
            {
                Left.Set(Main.mouseX - offset.X, 0f);
                Top.Set(Main.mouseY - offset.Y, 0f);
                Recalculate();
            }

            var parentSpace = Parent.GetDimensions().ToRectangle();
            if (!GetDimensions().ToRectangle().Intersects(parentSpace))
            {
                Left.Pixels = Utils.Clamp(Left.Pixels, 0, parentSpace.Right - Width.Pixels);
                Top.Pixels = Utils.Clamp(Top.Pixels, 0, parentSpace.Bottom - Height.Pixels);
                Recalculate();
            }
        }

        public override void Click(UIMouseEvent evt)
        {
            Main.PlaySound(SoundID.MenuTick);

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
            string paintName = "";
            if (PaintColor == 0)
            {
                Main.LocalPlayer.autoPaint = false;
                Main.LocalPlayer.builderAccStatus[3] = 1;
            }
            else
            {
                var paintItem = new Item();
                paintItem.SetDefaults(PaintID2ItemID(PaintColor));
                paintName = paintItem.Name;

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
                    Main.NewText(string.Format("You don\'t have {0}.", paintName));
                }
            }

            if (index != -1)
            {
                Utils.Swap(ref Main.LocalPlayer.inventory[index], ref Main.LocalPlayer.inventory[1]);
                Main.LocalPlayer.selectedItem = 1;
            }
            if (paintIndex != -1)
            {
                Utils.Swap(ref Main.LocalPlayer.inventory[paintIndex], ref Main.LocalPlayer.inventory[0]);
                Main.LocalPlayer.autoPaint = true;
                Main.LocalPlayer.builderAccStatus[3] = 0;
            }
            parentUI.selectedPanel = this;
            string text = PaintColor == 0 ? PaletteItem.Name : PaletteItem.Name + "\n" + paintName;
            parentUI.tileInfoText.SetText(text);

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
                if (item.modItem == null)
                {
                    id = MapHelper.tileLookup[item.createTile];
                }
                else
                {
                    try
                    {
                        var entries = ((IEnumerable)tileEntries.GetValue(null));
                        PropertyInfo keyInfo = null;
                        PropertyInfo valueInfo = null;
                        foreach (object entry in entries)
                        {
                            if (keyInfo == null)
                            {
                                keyInfo = entry.GetType().GetProperty("Key");
                                valueInfo = entry.GetType().GetProperty("Value");
                            }
                            if ((ushort)keyInfo.GetValue(entry) == item.createTile)
                            {
                                var entryList = (IEnumerable)valueInfo.GetValue(entry);
                                foreach (var trueEntry in entryList)
                                {
                                    return (Color)trueEntry.GetType().GetField("color", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(trueEntry);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        return Color.Transparent;
                    }
                }
            }
            if (item.createWall != -1)
            {
                if (item.modItem == null)
                {
                    id = MapHelper.wallLookup[item.createWall];
                }
                else
                {
                    try
                    {
                        var entries = (IEnumerable)wallEntries.GetValue(null);
                        PropertyInfo keyInfo = null;
                        PropertyInfo valueInfo = null;
                        foreach (object entry in entries)
                        {
                            if (keyInfo == null)
                            {
                                keyInfo = entry.GetType().GetProperty("Key");
                                valueInfo = entry.GetType().GetProperty("Value");
                            }
                            if ((ushort)keyInfo.GetValue(entry) == item.createWall)
                            {
                                var entryList = (IEnumerable)valueInfo.GetValue(entry);
                                foreach (var trueEntry in entryList)
                                {
                                    return (Color)trueEntry.GetType().GetField("color", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(trueEntry);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        return Color.Transparent;
                    }
                }
            }
            return colorLookup[id];
        }
    }
}