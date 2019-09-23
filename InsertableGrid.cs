using System;
using Microsoft.Xna.Framework;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace ColorPalette
{
    class InsertableGrid : UIGrid
    {
        protected static readonly Type innerGridType = typeof(UIGrid).GetNestedType("UIInnerList", System.Reflection.BindingFlags.NonPublic);
        protected UIElement inner;

        public InsertableGrid() : base()
        {
            inner = Elements.Find(x => x.GetType() == innerGridType);
        }

        public override bool Remove(UIElement item)
        {
            inner.RemoveChild(item);
            _scrollbar?.SetView(base.GetInnerDimensions().Height, GetTotalHeight());
            return this._items.Remove(item);
        }

        public override void Add(UIElement item)
        {
            _items.Add(item);
            inner.Append(item);
            _scrollbar?.SetView(base.GetInnerDimensions().Height, GetTotalHeight());
            inner.Recalculate();
        }

        public void Insert(int index, UIElement element)
        {
            _items.Insert(index, element);
            inner.Append(element);
            _scrollbar?.SetView(base.GetInnerDimensions().Height, GetTotalHeight());
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
            if (inner?.HasChild(element) == true)
            {
                inner.RemoveChild(element);
                inner.Append(element);
                _scrollbar?.SetView(base.GetInnerDimensions().Height, GetTotalHeight());
                inner.Recalculate();
            }
        }
    }
}
