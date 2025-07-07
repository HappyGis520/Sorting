using Sorting.Interface;

namespace Sorting.Interface
{
    public static class ModelTransformer
    {
        public static RequetTaskParam ToRequestTaskParam2(this WorkTaskModel dto)
        {
            return new RequetTaskParam()
            {
                Sku = dto.SKU,
                Barcode = dto.Barcode,
                StationId = dto.StationId,
                Completed = dto.Quantity
            };
        }
    }
}
