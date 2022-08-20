// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class Product
  {
    public readonly string Description;
    public readonly string FormattedPrice;
    public readonly string Name;
    public readonly string Sku;


    public Product(IntPtr o)
    {
      Description = CAPI.ovr_Product_GetDescription(o);
      FormattedPrice = CAPI.ovr_Product_GetFormattedPrice(o);
      Name = CAPI.ovr_Product_GetName(o);
      Sku = CAPI.ovr_Product_GetSKU(o);
    }
  }

  public class ProductList : DeserializableList<Product> {
    public ProductList(IntPtr a) {
      var count = (int)CAPI.ovr_ProductArray_GetSize(a);
      _Data = new List<Product>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new Product(CAPI.ovr_ProductArray_GetElement(a, (UIntPtr)i)));
      }

      _NextUrl = CAPI.ovr_ProductArray_GetNextUrl(a);
    }

  }
}
