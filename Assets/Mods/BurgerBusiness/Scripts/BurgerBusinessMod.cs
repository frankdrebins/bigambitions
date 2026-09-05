using System.Threading.Tasks;
using BAModAPI;
using BAModAPI.Services;
using BigAmbitions.Items;
using Buildings;
using Helpers;
using UnityEngine;

[assembly: RegisterModClass(typeof(BurgerBusiness.BurgerBusinessMod))]

namespace BurgerBusiness
{
    [ModEntryOnInitializationLoad]
    public sealed class BurgerBusinessMod : IModBigAmbitions
    {
        private const string BundleKey =
            "AssetBundles/burgerbusiness.unity3d";

        private const string ClassicBurgerAssetPath =
            "Assets/Mods/BurgerBusiness/ClassicBurger.asset";

        private const string BurgerRestaurantAssetPath =
            "Assets/Mods/BurgerBusiness/BurgerRestaurant.asset";

        private Item _classicBurger;
        private BusinessType _burgerRestaurant;

        public string[] RelativeAssetBundlePaths => new[] { BundleKey };

        public Task OnLoadAsync(ModContext context)
        {
            AssetBundle bundle = AssetService.GetBundle(
                context.ModId,
                BundleKey);

            _classicBurger =
                bundle.LoadAsset<Item>(ClassicBurgerAssetPath);

            if (_classicBurger != null)
                ItemsGetter.RegisterModItem(_classicBurger);

            _burgerRestaurant =
                bundle.LoadAsset<BusinessType>(BurgerRestaurantAssetPath);

            if (_burgerRestaurant != null)
                ModdingAPI.RegisterModBusinessType(_burgerRestaurant);

            context.Logger.Info(
                "Burger Restaurant and Classic Burger registered.");

            return Task.CompletedTask;
        }

        public Task OnUnloadAsync()
        {
            if (_burgerRestaurant != null)
                ModdingAPI.UnregisterModBusinessType(_burgerRestaurant);

            if (_classicBurger != null)
                ItemsGetter.UnregisterModItem(_classicBurger.itemName);

            return Task.CompletedTask;
        }
    }
}