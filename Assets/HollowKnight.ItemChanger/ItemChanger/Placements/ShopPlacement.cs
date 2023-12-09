﻿﻿using ItemChanger.Locations;
using ItemChanger.Util;
using ItemChanger.Components;
using ItemChanger.Internal.Preloaders;
using ItemChanger.Internal;

namespace ItemChanger.Placements
{
    /// <summary>
    /// Interface which exposes the stock methods of ShopPlacement for use by ShopLocation.
    /// </summary>
    public interface IShopPlacement
    {
        GameObject[] GetNewStock(GameObject[] oldStock, GameObject prefab);
        GameObject[] GetNewAltStock(GameObject[] newStock, GameObject[] altStock, GameObject prefab);
    }

    /// <summary>
    /// Placement which handles ShopLocation. Its main role is to handle adding its items to the shop stock as objects with the ModShopItemStats component.
    /// </summary>
    public class ShopPlacement : AbstractPlacement, IShopPlacement, IMultiCostPlacement, IPrimaryLocationPlacement
    {
        public ShopPlacement(string Name) : base(Name) { }

        public ShopLocation Location;
        AbstractLocation IPrimaryLocationPlacement.Location => Location;
        public override string MainContainerType => Container.Shop;

        protected override void OnLoad()
        {
            Location.Placement = this;
            Location.Load();
        }

        protected override void OnUnload()
        {
            Location.Unload();
        }

        public string requiredPlayerDataBool
        { 
            get => Location.requiredPlayerDataBool; 
            set => Location.requiredPlayerDataBool = value; 
        }
        public bool dungDiscount
        {
            get => Location.dungDiscount;
            set => Location.dungDiscount = value;
        }
        public DefaultShopItems defaultShopItems
        {
            get => Location.defaultShopItems;
            set => Location.defaultShopItems = value;
        }

        [Obsolete]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        public override void OnPreview(string previewText)
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
        {
            LogError("OnPreview is not supported on ShopPlacement.");
        }

        public void OnPreviewBatch(IEnumerable<(string, AbstractItem item)> ps)
        {
            Tags.MultiPreviewRecordTag recordTag = GetOrAddTag<Tags.MultiPreviewRecordTag>();
            recordTag.previewTexts ??= new string[Items.Count];
            foreach ((string previewText, AbstractItem item) in ps)
            {
                int i = Items.IndexOf(item);
                if (i < 0) continue;
                recordTag.previewTexts[i] = previewText;
            }
            AddVisitFlag(VisitState.Previewed);
        }

        public void AddItemWithCost(AbstractItem item, Cost cost)
        {
            CostTag tag = item.GetTag<CostTag>() ?? item.AddTag<CostTag>();
            tag.Cost = cost;
            Items.Add(item);
        }

        public void AddItemWithCost(AbstractItem item, int geoCost)
        {
            AddItemWithCost(item, Cost.NewGeoCost(geoCost));
        }

        public GameObject[] GetNewStock(GameObject[] oldStock, GameObject shopPrefab)
        {
            List<GameObject> stock = new(oldStock.Length + Items.Count());
            void AddShopItem(AbstractItem item)
            {
                GameObject shopItem = UObject.Instantiate(shopPrefab);
                shopItem.SetActive(false);
                ApplyItemDef(shopItem.GetComponent<ShopItemStats>(), item, item.GetTag<CostTag>()?.Cost);
                stock.Add(shopItem);
            }

            foreach (var item in Items.Where(i => !i.WasEverObtained())) AddShopItem(item);
            foreach (var item in Items.Where(i => i.WasEverObtained())) AddShopItem(item); // display refreshed items below unobtained items

            stock.AddRange(oldStock.Where(g => KeepOldItem(g.GetComponent<ShopItemStats>())));

            return stock.ToArray();
        }

        public GameObject[] GetNewAltStock(GameObject[] newStock, GameObject[] altStock, GameObject shopPrefab)
        {
            return newStock.Union(altStock.Where(g => KeepOldItem(g.GetComponent<ShopItemStats>()))).ToArray();
        }

        public void ApplyItemDef(ShopItemStats stats, AbstractItem item, Cost? cost)
        {
            foreach (var m in stats.gameObject.GetComponents<ModShopItemStats>()) UObject.Destroy(m); // Probably not necessary
            
            CostDisplayer costDisplayer = Location.costDisplayerSelectionStrategy.GetCostDisplayer(item);

            var mod = stats.gameObject.AddComponent<ModShopItemStats>();
            mod.item = item;
            mod.cost = cost;
            mod.costDisplayer = costDisplayer;
            mod.placement = this;

            // Apply all the stored values
            stats.playerDataBoolName = string.Empty;
            stats.nameConvo = string.Empty;
            stats.descConvo = string.Empty;
            stats.requiredPlayerDataBool = requiredPlayerDataBool;
            stats.removalPlayerDataBool = string.Empty;
            stats.dungDiscount = dungDiscount;
            stats.notchCostBool = string.Empty;
            

            // Need to set all these to make sure the item doesn't break in one of various ways
            stats.priceConvo = string.Empty;
            stats.specialType = 0;
            stats.charmsRequired = 0;
            stats.relic = false;
            stats.relicNumber = 0;
            stats.relicPDInt = string.Empty;

            // Apply the sprite for the UI
            stats.transform.Find("Item Sprite").gameObject.GetComponent<SpriteRenderer>().sprite = item.GetResolvedUIDef(this)!.GetSprite();

            Sprite? costSprite = costDisplayer.CustomCostSprite?.GetValue();

            if (costSprite != null)
            {
                stats.transform.Find("Geo Sprite").gameObject.GetComponent<SpriteRenderer>().sprite = costSprite;
            }
        }

        public bool KeepOldItem(ShopItemStats stats)
        {
            DefaultShopItems? itemType = ShopUtil.GetVanillaShopItemType(Location.UnsafeSceneName, stats);
            if (itemType == null) return true; // unrecognized items are kept by default
            if (itemType == DefaultShopItems.SalubraBlessing && (defaultShopItems & DefaultShopItems.SalubraNotches) == 0)
            {
                stats.requiredPlayerDataBool = string.Empty; // vanilla blessing appears immediately when notches are not vanilla
            }
            return (itemType & defaultShopItems) == itemType;
        }

        public override IEnumerable<Tag> GetPlacementAndLocationTags()
        {
            return base.GetPlacementAndLocationTags().Concat(Location.tags ?? Enumerable.Empty<Tag>());
        }
    }
}
