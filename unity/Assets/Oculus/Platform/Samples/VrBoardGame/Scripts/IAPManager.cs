namespace Oculus.Platform.Samples.VrBoardGame
{
	using UnityEngine;
	using Oculus.Platform;
	using Oculus.Platform.Models;
	using UnityEngine.UI;


	// This class coordinates In-App-Purchases (IAP) for the application.  Follow the
	// instructions in the Readme for setting up IAP on the Oculus Dashboard.  Only
	// one consumable IAP item is used is the demo: the Power-Ball!
	public class IAPManager : MonoBehaviour
	{
		// the game controler to notify when the user purchaes more powerballs
		[SerializeField] private GameController m_gameController = null;

		// where to record to display the current price for the IAP item
		[SerializeField] private Text m_priceText = null;

		// purchasable IAP products we've configured on the Oculus Dashboard
		private const string CONSUMABLE_1 = "PowerballPack1";

		void Start()
		{
			FetchProductPrices();
			FetchPurchasedProducts();
		}

		// get the current price for the configured IAP item
		public void FetchProductPrices()
		{
			string[] skus = { CONSUMABLE_1 };
			IAP.GetProductsBySKU(skus).OnComplete(GetProductsBySKUCallback);
		}

		void GetProductsBySKUCallback(Message<ProductList> msg)
		{
			if (msg.IsError)
			{
				PlatformManager.TerminateWithError(msg);
				return;
			}

			foreach (Product p in msg.GetProductList())
			{
				Debug.LogFormat("Product: sku:{0} name:{1} price:{2}", p.Sku, p.Name, p.FormattedPrice);
				if (p.Sku == CONSUMABLE_1)
				{
					m_priceText.text = p.FormattedPrice;
				}
			}
		}

		// fetches the Durable purchased IAP items.  should return none unless you are expanding the
		// to sample to include them.
		public void FetchPurchasedProducts()
		{
			IAP.GetViewerPurchases().OnComplete(GetViewerPurchasesCallback);
		}

		void GetViewerPurchasesCallback(Message<PurchaseList> msg)
		{
			if (msg.IsError)
			{
				PlatformManager.TerminateWithError(msg);
				return;
			}

			foreach (Purchase p in msg.GetPurchaseList())
			{
				Debug.LogFormat("Purchased: sku:{0} granttime:{1} id:{2}", p.Sku, p.GrantTime, p.ID);
			}
		}

		public void BuyPowerBallsPressed()
		{
#if UNITY_EDITOR
			m_gameController.AddPowerballs(1);
#else
			IAP.LaunchCheckoutFlow(CONSUMABLE_1).OnComplete(LaunchCheckoutFlowCallback);
#endif
		}

		private void LaunchCheckoutFlowCallback(Message<Purchase> msg)
		{
			if (msg.IsError)
			{
				PlatformManager.TerminateWithError(msg);
				return;
			}

			Purchase p = msg.GetPurchase();
			Debug.Log("purchased " + p.Sku);
			m_gameController.AddPowerballs(3);
		}
	}
}
