using System.Threading.Tasks;

namespace Sorting.Interface
{
    public interface IWesService
    {
        /// <summary>
        /// 查询商品信息
        /// </summary>
        /// <param name="param"></param>
       Task< ResponseBase<GoodsList>> QueryGoodsInfo(QueryGoodsParam param);
        /// <summary>
        /// 请求分配分拣任务
        /// </summary>
        /// <param name="taskInfoVosInputDto"></param>
       Task<ResponseBase<SupplyTask>> RequetSupplyTask(RequetTaskParam taskInfoVosInputDto);
    }
}
