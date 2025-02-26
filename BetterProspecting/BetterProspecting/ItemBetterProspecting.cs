using System;
using System.Linq;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

using ThisMod = BetterProspecting.BetterProspectingModSystem;

namespace BetterProspecting
{
    public class ItemBetterProspecting : ItemProspectingPick
    {
        SkillItem[]? toolModes;

        public override void OnLoaded(ICoreAPI api)
        {
            //I need to add assets!

            toolModes = ObjectCacheUtil.GetOrCreate(api, "proPickToolModes", () =>
            {
                SkillItem[] modes;
                modes = new SkillItem[]{
                    new() { Code = new AssetLocation("density"), Name = Lang.Get("Density Search Mode (Long range, chance based search)") },
                    new() { Code = new AssetLocation("distance_short"), Name = Lang.Get("Short Distance Mode (Short range, distance search)") },
                    new() { Code = new AssetLocation("distance"), Name = Lang.Get("Distance Mode (Long range, distance search)") },
                    new() { Code = new AssetLocation("stone"), Name = Lang.Get("Stone Mode (Long range, distance search for stone)") },
                    new() { Code = new AssetLocation("area1"), Name = Lang.Get("Area Sample Mode (Searches in a small area)") },
                    new() { Code = new AssetLocation("area2"), Name = Lang.Get("Area Sample Mode (Searches in a medium area)") },
                    new() { Code = new AssetLocation("area3"), Name = Lang.Get("Area Sample Mode (Searches in a large area)") },
                };

                if (api is ICoreClientAPI capi)
                {
                    modes[0].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/heatmap.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[0].TexturePremultipliedAlpha = false;
                    modes[1].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("betterprospecting", "textures/icons/short.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[1].TexturePremultipliedAlpha = false;
                    modes[2].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("betterprospecting", "textures/icons/abpro_line.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[2].TexturePremultipliedAlpha = false;
                    modes[3].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("betterprospecting", "textures/icons/abpro_stone.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[3].TexturePremultipliedAlpha = false;
                    modes[4].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("betterprospecting", "textures/icons/abpro_small.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[4].TexturePremultipliedAlpha = false;
                    modes[5].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("betterprospecting", "textures/icons/abpro_med.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[5].TexturePremultipliedAlpha = false;
                    modes[6].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("betterprospecting", "textures/icons/abpro_large.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[6].TexturePremultipliedAlpha = false;
                }

                return modes;
            });

            base.OnLoaded(api);
        }

        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
        {
            return toolModes;
        }

        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
        {
            return Math.Min(toolModes.Length - 1, slot.Itemstack.Attributes.GetInt("toolMode"));
        }

        public override float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
        {
            float remain = base.OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt, counter);
            int toolMode = GetToolMode(itemslot, player, blockSel);

            remain = (remain + remainingResistance) / 2.2f;
            return remain;
        }

        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1)
        {
            int toolMode = GetToolMode(itemslot, (byEntity as EntityPlayer).Player, blockSel);
            var damage = 1;
            var intialToolsPresent = 1;

            if (toolMode == intialToolsPresent + 0)
            {
                ProbeDistanceSampleMode(world, byEntity, itemslot, blockSel, ThisMod.Config.DistanceModeSmallRadius, ProspectingTargetType.Ore);
            }
            else if (toolMode == intialToolsPresent + 1)
            {
                ProbeDistanceSampleMode(world, byEntity, itemslot, blockSel, ThisMod.Config.DistanceModeLargeRadius, ProspectingTargetType.Ore);
            }
            else if (toolMode == intialToolsPresent + 2)
            {
                ProbeDistanceSampleMode(world, byEntity, itemslot, blockSel, ThisMod.Config.DistanceModeStoneRadius, ProspectingTargetType.Rock);
            }
            else if (toolMode == intialToolsPresent + 3)
            {
                ProbeAreaSampleMode(world, byEntity, itemslot, blockSel, ThisMod.Config.AreaModeSmallRadius);
            }
            else if (toolMode == intialToolsPresent + 4)
            {
                ProbeAreaSampleMode(world, byEntity, itemslot, blockSel, ThisMod.Config.AreaModeMediumRadius);
            }
            else if (toolMode == intialToolsPresent + 5)
            {
                ProbeAreaSampleMode(world, byEntity, itemslot, blockSel, ThisMod.Config.AreaModeLargeRadius);
            }
            else
            {
                damage = 1;
                ProbeBlockDensityMode(world, byEntity, itemslot, blockSel);
            }

            /// only do extra damage if not using the game's built in <see cref="ItemProspectingPick.ProbeBlockDensityMode"/>
            if (DamagedBy != null && DamagedBy.Contains(EnumItemDamageSource.BlockBreaking))
            {
                DamageItem(world, byEntity, itemslot, damage);
            }

            return true;
        }

        protected virtual void ProbeAreaSampleMode(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, int xzlength)
        {
            int ylength = ThisMod.Config.ChunkMode ? ThisMod.Config.ChunkModeHeight : xzlength;

            IPlayer? byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = world.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

            Block block = world.BlockAccessor.GetBlock(blockSel.Position);
            block.OnBlockBroken(world, blockSel.Position, byPlayer, 0);

            if (!isPropickable(block)) return;

            IServerPlayer? serverPlayer = byPlayer as IServerPlayer;
            if (serverPlayer == null) return;

            serverPlayer.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.GetL(serverPlayer.LanguageCode, $"Area sample taken for a length of {xzlength}:"), EnumChatType.Notification);

            Dictionary<string, int> quantityFound = new Dictionary<string, int>();

            api
                .World
                .GetCachingBlockAccessor(false, false)
                .WalkBlocks(blockSel.Position.AddCopy(xzlength, ylength, xzlength), blockSel.Position.AddCopy(-xzlength, -ylength, -xzlength), delegate (Block nblock, int x, int y, int z)
            {
                if (nblock.BlockMaterial == EnumBlockMaterial.Ore && nblock.Variant.ContainsKey("type"))
                {
                    string key = "ore-" + nblock.Variant["type"];
                    int value = 0;
                    quantityFound.TryGetValue(key, out value);
                    quantityFound[key] = value + 1;
                }
            });
            List<KeyValuePair<string, int>> list = quantityFound.OrderByDescending((KeyValuePair<string, int> val) => val.Value).ToList();
            if (list.Count == 0)
            {
                serverPlayer.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.GetL(serverPlayer.LanguageCode, "No ore node nearby"), EnumChatType.Notification);
                return;
            }

            serverPlayer.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.GetL(serverPlayer.LanguageCode, "Found the following ore nodes"), EnumChatType.Notification);
            foreach (KeyValuePair<string, int> item in list)
            {
                string l = Lang.GetL(serverPlayer.LanguageCode, item.Key);
                string l2 = Lang.GetL(serverPlayer.LanguageCode, resultTextByQuantity(item.Value), Lang.Get(item.Key));
                serverPlayer.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.GetL(serverPlayer.LanguageCode, l2, l), EnumChatType.Notification);
            }
        }

        protected virtual void ProbeDistanceSampleMode(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, int xzlength, ProspectingTargetType mode)
        {
            int ylength = ThisMod.Config.ChunkMode ? ThisMod.Config.ChunkModeHeight : xzlength;


            IPlayer? byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = world.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

            Block block = world.BlockAccessor.GetBlock(blockSel.Position);
            block.OnBlockBroken(world, blockSel.Position, byPlayer, 0);

            if (!isPropickable(block)) return;

            IServerPlayer? serverPlayer = byPlayer as IServerPlayer;
            if (serverPlayer == null) return;

            serverPlayer.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.GetL(serverPlayer.LanguageCode, $"Area sample taken for a length of {xzlength}:"), EnumChatType.Notification);

            Dictionary<string, BlockPos> closestByType = new Dictionary<string, BlockPos>();

            BlockPos blockPos = blockSel.Position.Copy();
            
            api
                .World
                .GetCachingBlockAccessor(false, false)
                .WalkBlocks(blockPos.AddCopy(xzlength, ylength, xzlength), blockPos.AddCopy(-xzlength, -ylength, -xzlength), delegate (Block nblock, int x, int y, int z)
            {
                if (mode == ProspectingTargetType.Ore && nblock.BlockMaterial == EnumBlockMaterial.Ore && nblock.Variant.ContainsKey("type"))
                {
                    closestByType.TryAdd(nblock.Variant["type"].ToUpper(), new BlockPos(x, y, z));
                }
                else if (mode == ProspectingTargetType.Rock && nblock.Variant.ContainsKey("rock"))
                {
                    closestByType.TryAdd(nblock.Variant["rock"].ToUpper(), new BlockPos(x, y, z));
                }

            }, true);

            List<KeyValuePair<string, BlockPos>> list = closestByType.ToList();
            if (list.Count == 0)
            {
                serverPlayer.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.GetL(serverPlayer.LanguageCode, "No ore node nearby"), EnumChatType.Notification);
                return;
            }

            serverPlayer.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.GetL(serverPlayer.LanguageCode, "Found the following ore nodes"), EnumChatType.Notification);
            foreach (KeyValuePair<string, BlockPos> item in list)
            {
                serverPlayer.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.GetL(serverPlayer.LanguageCode, $"{item.Key}: {Math.Round(item.Value.DistanceTo(blockSel.Position))} block(s) away"), EnumChatType.Notification);
            }
        }

        private bool isPropickable(Block block)
        {
            return block?.Attributes?["propickable"].AsBool(false) == true;
        }

        
        protected enum ProspectingTargetType {
            Rock,
            Ore,
        }
    }
}
