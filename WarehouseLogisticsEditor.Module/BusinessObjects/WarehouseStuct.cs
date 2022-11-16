using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.DashboardCommon;
using DevExpress.Data.Filtering;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace WarehouseLogisticsEditor.Module.BusinessObjects
{

    #region Склад
    [NavigationItem("WarehouseEditor")]
    public class Warehouse : BaseObject  
      
    {

        public Warehouse(Session session) : base(session) { }
        string warehouseName;
        public string WarehouseName                //TODO имя склада (произвольное). Позже наложим ограничения
        {
            get { return warehouseName; }
            set { SetPropertyValue(nameof(WarehouseName), ref warehouseName, value); }
        }

        int warehouseNumber;                       //TODO  Номер склада. Позже наложим ограничения
        public int WarehouseNumber
        {
            get { return warehouseNumber; }
            set { SetPropertyValue(nameof(WarehouseNumber), ref warehouseNumber, value); }
        }

        private int? warehousPegCount = null;      // количество пикетов на складе. Вычисляемое. 
        public int? WarehousPegCount
        {
            get { if
                (!IsLoading && !IsSaving && warehousPegCount == null)
                    UpdateWarehousPegCount(false); return warehousPegCount;
                    }
           // set { SetPropertyValue(nameof(WarehousPegCount), ref WarehousPegCount , value); }
        }
        public void UpdateWarehousPegCount (bool forseChageEvents)  //UNDONE
        {
            int? oldWarehousePegCount = warehousPegCount;
            warehousPegCount = Convert.ToInt32(Evaluate(CriteriaOperator.Parse("WrehousePegs.Count")));
            if (forseChageEvents)
                OnChanged(nameof(WarehousPegCount), oldWarehousePegCount, warehousPegCount);
        }




        [Association("Warehouse-WrehouseAreas"), Aggregated]
        public XPCollection<WrehouseArea> WrehouseAreas                      // коллекция зарегистрированных на складе площадок
        {
            get { return GetCollection<WrehouseArea>(nameof(WrehouseAreas)); }
        }

        [Association("Warehouse-WrehousePegs"), Aggregated]
        public XPCollection<WrehousePeg> WrehousePegs                      // коллекция зарегистрированных на складе пикетов
        {
            get { return GetCollection<WrehousePeg>(nameof(WrehousePegs)); }
        }

    }

    #endregion



    #region Площадка склада

   // [NavigationItem("WarehouseEditor")]
    public class WrehouseArea : BaseObject              //
    {

        public WrehouseArea(Session session) : base(session)
        {
            createdOn = DateTime.Now;                    //HACK установка даты (поменять алгоритм вычисления даты)
        }
        string areaName;
        public string AreaName                          // имя площадки,- поменять на автоматически вычисляемое ( оставить алиас для удобства)
        {
            get { return areaName; }
            set { SetPropertyValue(nameof(AreaName), ref areaName, value); }
        }



        int cargoOnArea;                    // груз на площадке
        public int CargoOnArea
        {
            get { return cargoOnArea; }
            set { SetPropertyValue(nameof(CargoOnArea), ref cargoOnArea, value); }
        }

        [VisibleInListView(false)]
        public DateTime CreatedOn
        {
            get { return createdOn; }
            internal set { SetPropertyValue(nameof(CreatedOn), ref createdOn, value); }
        }
        DateTime createdOn; //HACK дата создания площадки.
        DateTime deleteOn;  //HACK дата удаления площадки.

        Warehouse warehouse;                         // ссылка на склад
        [Association("Warehouse-WrehouseAreas")]
        public Warehouse Warehouse
        {

            get { return warehouse; }
            set { SetPropertyValue(nameof(Warehouse), ref warehouse, value); }
        }

        [Association("WrehouseArea-WrehousePegs")]
        public XPCollection<WrehousePeg> WrehousePegs                      // коллекция зарегистрированных на складе площадок
        {
            get { return GetCollection<WrehousePeg>(nameof(WrehousePegs)); }  //TODO возвращать только коллекцию, относящиюся к складу площадки
        }

    }

    #endregion


    #region Пикеты на складе

    //[NavigationItem("WarehouseEditor")]
    public class WrehousePeg : BaseObject
    {
        public WrehousePeg(Session session) : base(session) { }

        int pegNumber;
        //[PersistentAlias("Warehouse.warehouseNumber* 100 + Warehouse.WarehousPegCount")]                 // 
        public int PegNumber                                   //TODO номер пикета. Будет вычисляться при создании. Определиться с обновлением значения??
        {
            get { return pegNumber; }
            //get { return (int) (EvaluateAlias(nameof (PegNumber)) ?? 0) ; }
            set { SetPropertyValue(nameof(PegNumber), ref pegNumber, value); }
        }

        Warehouse warehouse;                   // ссылка на склад
        [Association("Warehouse-WrehousePegs")]
        public Warehouse Warehouse
        {
            get { return warehouse; }
            set { SetPropertyValue(nameof(Warehouse), ref warehouse, value); }
        }

        WrehouseArea wrehouseArea;            // ссылка на площадку
        [Association("WrehouseArea-WrehousePegs")]
        public WrehouseArea WrehouseArea
        {

            get { return wrehouseArea; }
            set { SetPropertyValue(nameof(WrehouseArea), ref wrehouseArea, value); }
        }
    }

    #endregion
}
