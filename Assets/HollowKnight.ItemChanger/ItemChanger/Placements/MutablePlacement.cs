﻿using ItemChanger.Locations;

namespace ItemChanger.Placements
{
    /// <summary>
    /// The default placement for most use cases.
    /// Chooses an item container for its location based on its item list.
    /// </summary>
    public class MutablePlacement : AbstractPlacement, IContainerPlacement, ISingleCostPlacement, IPrimaryLocationPlacement
    {
        public MutablePlacement(string Name) : base(Name) { }

        public ContainerLocation Location;
        AbstractLocation IPrimaryLocationPlacement.Location => Location;

        public override string MainContainerType => containerType;
        public string containerType = Container.Unknown;

        public Cost? Cost { get; set; }

        protected override void OnLoad()
        {
            Location.Placement = this;
            Location.Load();
            Cost?.Load();
        }

        protected override void OnUnload()
        {
            Location.Unload();
            Cost?.Unload();
        }

        public void GetContainer(AbstractLocation location, out GameObject obj, out string containerType)
        {
            if (this.containerType == Container.Unknown)
            {
                this.containerType = ChooseContainerType(this, location as ContainerLocation, Items);
            }
            
            containerType = this.containerType;
            var container = Container.GetContainer(containerType);
            if (container == null || !container.SupportsInstantiate)
            {
                this.containerType = containerType = ChooseContainerType(this, location as ContainerLocation, Items);
                container = Container.GetContainer(containerType);
                if (container == null) throw new InvalidOperationException($"Unable to resolve container type {containerType} for placement {Name}!");
            }

            obj = container.GetNewContainer(new ContainerInfo(container.Name, this, location.flingType, Cost,
                location.GetTags<Tags.ChangeSceneTag>().FirstOrDefault()?.ToChangeSceneInfo()));
        }

        public static string ChooseContainerType(ISingleCostPlacement placement, ContainerLocation? location, IEnumerable<AbstractItem> items)
        {
            if (location?.forceShiny ?? true)
            {
                return Container.Shiny;
            }

            bool mustSupportCost = placement.Cost != null;
            bool mustSupportSceneChange = location.GetTags<Tags.ChangeSceneTag>().Any() || ((AbstractPlacement)placement).GetTags<Tags.ChangeSceneTag>().Any();

            HashSet<string> unsupported = new(((placement as AbstractPlacement)?.GetPlacementAndLocationTags() ?? Enumerable.Empty<Tag>())
                .OfType<Tags.UnsupportedContainerTag>()
                .Select(t => t.containerType));

            string containerType = items
                .Select(i => i.GetPreferredContainer())
                .FirstOrDefault(c => location.Supports(c) && !unsupported.Contains(c) && Container.SupportsAll(c, true, mustSupportCost, mustSupportSceneChange));

            if (string.IsNullOrEmpty(containerType))
            {
                if (((placement as AbstractPlacement)?.GetPlacementAndLocationTags() ?? Enumerable.Empty<Tag>())
                    .OfType<Tags.PreferredDefaultContainerTag>().FirstOrDefault() is Tags.PreferredDefaultContainerTag t
                    && Container.SupportsAll(t.containerType, true, mustSupportCost, mustSupportSceneChange))
                {
                    containerType = t.containerType;
                }
                else if (!mustSupportCost && !mustSupportSceneChange && !unsupported.Contains(Container.Chest)
                    && items.Skip(1).Any()) // has more than 1 item, and can support Chest

                {
                    containerType = Container.Chest;
                }
                else
                {
                    containerType = Container.Shiny;
                }
            }

            return containerType;
        }

        public override IEnumerable<Tag> GetPlacementAndLocationTags()
        {
            return base.GetPlacementAndLocationTags().Concat(Location.tags ?? Enumerable.Empty<Tag>());
        }
    }
}
