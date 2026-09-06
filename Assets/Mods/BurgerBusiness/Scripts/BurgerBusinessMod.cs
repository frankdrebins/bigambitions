using System;
using System.Collections;
using System.Reflection;
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
        private GameObject _competitorBridgeObject;

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

            _competitorBridgeObject =
                new GameObject("BurgerBusiness Competitor Bridge");

            UnityEngine.Object.DontDestroyOnLoad(
                _competitorBridgeObject);

            BurgerCompetitorBridge bridge =
                _competitorBridgeObject.AddComponent<
                    BurgerCompetitorBridge>();

            bridge.Initialize(context);

            context.Logger.Info(
                "Burger Restaurant and Classic Burger registered.");

            return Task.CompletedTask;
        }

        public Task OnUnloadAsync()
        {
            if (_competitorBridgeObject != null)
            {
                UnityEngine.Object.Destroy(
                    _competitorBridgeObject);

                _competitorBridgeObject = null;
            }

            if (_burgerRestaurant != null)
                ModdingAPI.UnregisterModBusinessType(_burgerRestaurant);

            if (_classicBurger != null)
                ItemsGetter.UnregisterModItem(_classicBurger.itemName);

            return Task.CompletedTask;
        }
    }

    public sealed class BurgerCompetitorBridge : MonoBehaviour
    {
        private const string BurgerObjectName =
            "BurgerRestaurant";

        private const string FastFoodObjectName =
            "FastFoodRestaurant";

        private ModContext _context;

        private Type _startBusinessUiType;
        private Type _startBusinessTypeUiType;

        private FieldInfo _businessTypesField;
        private FieldInfo _cardBusinessTypeField;

        private MethodInfo _getCompetitorsMethod;
        private MethodInfo _setCompetitorsAmountMethod;

        private BusinessType _fastFoodBusinessType;

        private float _nextRefreshTime;
        private bool _ready;
        private bool _reportedApplication;
        private bool _reportedFailure;

        public void Initialize(ModContext context)
        {
            _context = context;

            try
            {
                _startBusinessUiType = FindType(
                    "UI.Smartphone.Apps.BizMan.StartBusiness." +
                    "StartBusinessUI");

                _startBusinessTypeUiType = FindType(
                    "UI.Smartphone.Apps.BizMan.StartBusiness." +
                    "StartBusinessTypeUI");

                if (_startBusinessUiType == null ||
                    _startBusinessTypeUiType == null)
                {
                    throw new InvalidOperationException(
                        "Start Business UI types were not found.");
                }

                BindingFlags flags =
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance;

                _businessTypesField =
                    _startBusinessUiType.GetField(
                        "_businessTypes",
                        flags);

                _cardBusinessTypeField =
                    _startBusinessTypeUiType.GetField(
                        "_businessType",
                        flags);

                _getCompetitorsMethod =
                    _startBusinessUiType.GetMethod(
                        "GetCompetitors",
                        flags);

                _setCompetitorsAmountMethod =
                    _startBusinessTypeUiType.GetMethod(
                        "SetCompetitorsAmount",
                        flags);

                if (_businessTypesField == null ||
                    _cardBusinessTypeField == null ||
                    _getCompetitorsMethod == null ||
                    _setCompetitorsAmountMethod == null)
                {
                    throw new MissingMemberException(
                        "One or more competitor UI members were not found.");
                }

                _ready = true;

                _context.Logger.Info(
                    "[BurgerBusiness] Competitor category bridge ready.");
            }
            catch (Exception exception)
            {
                ReportFailure(exception);
            }
        }

        private void Update()
        {
            if (!_ready)
                return;

            if (Time.unscaledTime < _nextRefreshTime)
                return;

            _nextRefreshTime = Time.unscaledTime + 0.5f;

            try
            {
                object startBusinessUi =
                    FindActiveStartBusinessUi();

                if (startBusinessUi == null)
                    return;

                IDictionary cards =
                    _businessTypesField.GetValue(
                        startBusinessUi) as IDictionary;

                if (cards == null || cards.Count == 0)
                    return;

                if (_fastFoodBusinessType == null)
                    _fastFoodBusinessType =
                        FindBusinessType(FastFoodObjectName);

                if (_fastFoodBusinessType == null)
                    return;

                foreach (DictionaryEntry entry in cards)
                {
                    object card = entry.Value;

                    if (card == null)
                        continue;

                    BusinessType cardBusinessType =
                        _cardBusinessTypeField.GetValue(
                            card) as BusinessType;

                    if (cardBusinessType == null ||
                        cardBusinessType.name != BurgerObjectName)
                    {
                        continue;
                    }

                    int burgerCompetitors =
                        Convert.ToInt32(
                            _getCompetitorsMethod.Invoke(
                                startBusinessUi,
                                new object[] { cardBusinessType }));

                    int fastFoodCompetitors =
                        Convert.ToInt32(
                            _getCompetitorsMethod.Invoke(
                                startBusinessUi,
                                new object[] { _fastFoodBusinessType }));

                    int combinedCompetitors =
                        burgerCompetitors + fastFoodCompetitors;

                    _setCompetitorsAmountMethod.Invoke(
                        card,
                        new object[] { combinedCompetitors });

                    if (!_reportedApplication)
                    {
                        _reportedApplication = true;

                        _context.Logger.Info(
                            "[BurgerBusiness] Competitor bridge applied. " +
                            "Burger: " +
                            burgerCompetitors +
                            ", Fast Food: " +
                            fastFoodCompetitors +
                            ", Combined: " +
                            combinedCompetitors);
                    }

                    return;
                }
            }
            catch (Exception exception)
            {
                ReportFailure(exception);
            }
        }

        private object FindActiveStartBusinessUi()
        {
            UnityEngine.Object[] instances =
                Resources.FindObjectsOfTypeAll(
                    _startBusinessUiType);

            foreach (UnityEngine.Object instance in instances)
            {
                Component component =
                    instance as Component;

                if (component != null &&
                    component.gameObject.activeInHierarchy)
                {
                    return instance;
                }
            }

            return null;
        }

        private static BusinessType FindBusinessType(
            string objectName)
        {
            BusinessType[] businessTypes =
                Resources.FindObjectsOfTypeAll<BusinessType>();

            foreach (BusinessType businessType in businessTypes)
            {
                if (businessType != null &&
                    businessType.name == objectName)
                {
                    return businessType;
                }
            }

            return null;
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in
                AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type =
                    assembly.GetType(fullName, false);

                if (type != null)
                    return type;
            }

            return null;
        }

        private void ReportFailure(Exception exception)
        {
            if (_reportedFailure || _context == null)
                return;

            _reportedFailure = true;

            _context.Logger.Info(
                "[BurgerBusiness] Competitor bridge failed: " +
                exception);
        }
    }
}