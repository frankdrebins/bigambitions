using System;
using System.Threading.Tasks;
using BAModAPI;

[assembly: RegisterModClass(typeof(BurgerBusiness.BurgerBusinessMod))]

namespace BurgerBusiness
{
    [ModEntryOnInitializationLoad]
    public sealed class BurgerBusinessMod : IModBigAmbitions
    {
        public string[] RelativeAssetBundlePaths => Array.Empty<string>();

        public Task OnLoadAsync(ModContext context)
        {
            context.Logger.Info("Burger Business mod loaded.");
            return Task.CompletedTask;
        }

        public Task OnUnloadAsync()
        {
            return Task.CompletedTask;
        }
    }
}