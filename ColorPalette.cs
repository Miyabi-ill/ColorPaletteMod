using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace ColorPalette
{
	public class ColorPalette : Mod
	{
        public static ModHotKey ColorPaletteToggleKey { get; private set; }
        public static ColorPalette instance;

        internal UserInterface userInterface;
        internal ColorPaletteUI colorPaletteUI;

        public override void Load()
        {
            instance = this;

            if (!Main.dedServ)
            {
                PaletteIO.ReadAllPalette();

                ColorPaletteToggleKey = RegisterHotKey("Toggle Color Palette", "Z");
                colorPaletteUI = new ColorPaletteUI();
                userInterface = new UserInterface();
                colorPaletteUI.Activate();
                userInterface.SetState(colorPaletteUI);
            }
        }

        public override void Unload()
        {
            ColorPaletteToggleKey = null;
            instance = null;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (ColorPaletteUI.Visible)
            {
                userInterface?.Update(gameTime);
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "ColorPalette: Palette UI",
                    delegate
                    {
                        if (ColorPaletteUI.Visible)
                        {
                            userInterface.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }
    }

    public class ColorPaletteUIPlayer : ModPlayer
    {
        public override void OnEnterWorld(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                List<ColorData> remove = new List<ColorData>();

                var ui = ((ColorPalette)mod).colorPaletteUI;
                ui.colorGrid.Clear();
                foreach (var data in ui.currentIO.datas)
                {
                    Item item = new Item();
                    if (data.id < ItemID.Count)
                    {
                        item.SetDefaults(data.id);
                    }
                    else
                    {
                        try
                        {
                            var split = data.fullName.Split(new char[] { '.' });
                            var modItem = ModLoader.GetMod(split[0])?.GetItem(split[1]);
                            if (modItem != null)
                            {
                                item = modItem.item;
                            }
                        }
                        catch (Exception e)
                        {
                            mod.Logger.Warn(string.Format("Something went wrong during loading item: {0}. This will remove.", data.fullName), e);
                            remove.Add(data);
                        }
                    }
                    if (item.type != 0)
                    {
                        var colorUI = new ColorPanel(item, data.paint)
                        {
                            colorData = data
                        };
                        colorUI.Initialize();
                        ui.colorGrid.Add(colorUI);
                    }
                }

                foreach (var rmv in remove)
                {
                    ((ColorPalette)mod).colorPaletteUI.currentIO.datas.Remove(rmv);
                }

                ui.colorGrid.Add(ui.createColorButton);
            }
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (player.whoAmI == Main.myPlayer && ColorPalette.ColorPaletteToggleKey.JustPressed)
            {
                ColorPaletteUI.Visible = !ColorPaletteUI.Visible;
            }
        }
    }

    public class PaletteIOWorld : ModWorld
    {
        public override TagCompound Save()
        {
            PaletteIO.SaveAllPalette();
            return base.Save();
        }
    }
}