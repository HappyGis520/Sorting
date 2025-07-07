using System.Collections.Generic;

namespace Sorting.Interface
{
    public class QueryGoodsParam
    {
        public string Barcode { get; set; }
    }
    public class Goods
    {
        public string Sku;
        public string SkuName;
        public string BarCode;
        public int AgvLoadingQty;
        public int Weight;
    }
    public class GoodsList
    {
        public List<Goods> Items = new List<Goods>();
    }
}
