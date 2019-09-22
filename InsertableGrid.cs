using System;
using Microsoft.Xna.Framework;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace ColorPalette
{
    class InsertableGrid : UIGrid
    {
        protected static readonly Type innerGridType = typeof(UIGrid).GetNestedType("UIInnerList", System.Reflection.BindingFlags.NonPublic);
        public void Insert(int index, UIElement element)
        {
            _items.Insert(index, element);
            var inner = Elements.Find(x => x.GetType() == innerGridType);
            inner.Append(element);
            inner.Recalculate();
        }

        public int IndexOf(UIElement element)
        {
            return _items.IndexOf(element);
        }
    }

    class LazyUpdatableGrid : InsertableGrid
    {
        public void LazyUpdateElement(UIElement element)
        {
            var inner = Elements.Find(x => x.GetType() == innerGridType);
            if (inner?.HasChild(element) == true)
            {
                inner.RemoveChild(element);
                inner.Append(element);
                inner.Recalculate();
            }
        }
    }
}
