using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.CodeParser;
using DevExpress.DashboardCommon;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.StateMachine.Utils;
using DevExpress.Pdf.Native.BouncyCastle.Asn1.Cms;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.PivotGrid.Criteria.Validation;
using DevExpress.Xpo;
using DevExpress.Persistent.Validation;
using DevExpress.XtraCharts;
using System.Drawing;

namespace WarehouseLogisticsEditor.Module.BusinessObjects
{

    #region Склад
    [NavigationItem("WarehouseEditor")]
    [DefaultClassOptions]
    [System.ComponentModel.DefaultProperty(nameof(Warehouse))]
    public class Warehouse : BaseObject

    {

        public Warehouse(Session session) : base(session) { }
        
        string warehouseName;
        [Size(128)]
        public string WarehouseName                                                                        //Имя склада (произвольное). Позже наложим ограничения
        {
            get { return warehouseName; }
            set { SetPropertyValue(nameof(WarehouseName), ref warehouseName, value); }
        }

        
        int warehouseNumber;                                                                                //TODO  Номер склада. Позже наложим ограничения

        public int WarehouseNumber
        {
            get { return warehouseNumber; }
            set { SetPropertyValue(nameof(WarehouseNumber), ref warehouseNumber, value); }
        }

        private int? warehousPegCount = null;                                                                // количество пикетов на складе. Вычисляемое. 
        public int? WarehousPegCount
        {
            get
            {
                if
                (!IsLoading && !IsSaving && warehousPegCount == null)
                    UpdateWarehousPegCount(false); return warehousPegCount;
            }
        }
        public void UpdateWarehousPegCount(bool forseChageEvents)  
        {
            int? oldWarehousePegCount = warehousPegCount;
            warehousPegCount = Convert.ToInt32(Evaluate(CriteriaOperator.Parse("WarehousePegs.Count")));
            if (forseChageEvents)
                OnChanged(nameof(WarehousPegCount), oldWarehousePegCount, warehousPegCount);
        }




        [Association("Warehouse-WarehouseAreas")]
        public XPCollection<WarehouseArea> WarehouseAreas                                                  // коллекция зарегистрированных на складе площадок
        {
            get { return GetCollection<WarehouseArea>(nameof(WarehouseAreas)); }

        }

        [Association("Warehouse-WarehousePegs"), Aggregated] 

        public XPCollection<WarehousePeg> WarehousePegs                                                      // коллекция зарегистрированных на складе пикетов
        {
            get { return GetCollection<WarehousePeg>(nameof(WarehousePegs)); }
            set { }
        }

    }

    #endregion



    #region Площадка склада

    [NavigationItem("WarehouseEditor")]
    [DefaultClassOptions]
    [RuleCriteria("Pegs.Count()-1 = Pegs.Max([PegNumber])-Pegs.Min([PegNumber])")]
    public class WarehouseArea : BaseObject              //
    {

        public WarehouseArea(Session session) : base(session)
        {
        
        }
        string areaName;

        public string AreaName                                                                              // имя площадки,- поменять на автоматически вычисляемое ( оставить алиас для удобства)
        { 
            get { return areaName; }
            set { SetPropertyValue(nameof(AreaName), ref areaName, value); }
        }

        [Association("WarehouseArea-CargoOnArea"), Aggregated]
        public XPCollection<CargoOnArea> CargosOnArea                                                     // коллекция зарегистрированных на складе грузов
        {
            get { return GetCollection<CargoOnArea>(nameof(CargosOnArea)); }
            set { }
        }



        DateTime createdOn;                                                                                // дата создания площадки.
        // [VisibleInListView(false)]
        [RuleRequiredField(DefaultContexts.Save)]
        public DateTime CreatedOn
        {
            get { return createdOn; }
            set { SetPropertyValue(nameof(CreatedOn), ref createdOn, value); }
        }

        DateTime deleteOn;                                                                                  // дата удаления площадки.

        [RuleRequiredField(DefaultContexts.Save)]
        public DateTime DeleteOn
        {
            get { return deleteOn; }
            set { SetPropertyValue(nameof(DeleteOn), ref deleteOn, value); }
        }


        Warehouse warehouse;                                                                                 // ссылка на склад
        [Association("Warehouse-WarehouseAreas")]

        public Warehouse Warehouse
        {
            get { return warehouse; }


            set
            {
                SetPropertyValue(nameof(Warehouse), ref warehouse, value);
                RefreshAvailablePegs();
            }



        }

        [Association("WarehouseArea-WarehousePegs")]
        [DataSourceProperty(nameof(AvailablePegs))]
        public XPCollection<WarehousePeg> Pegs
        {
            get
            {
                return GetCollection<WarehousePeg>(nameof(Pegs));
            }
        }

        private XPCollection<WarehousePeg> availablePegs;

        [Browsable(false)]
        public XPCollection<WarehousePeg> AvailablePegs                                                      // коллекция зарегистрированных на складе площадок
        {
            get
            {
                if (availablePegs == null)
                {
                    availablePegs = new XPCollection<WarehousePeg>(Session);

                }
                RefreshAvailablePegs();
                return availablePegs;
            }
        }
        private void RefreshAvailablePegs()                                                                 // обновление коллекции 
        {
            if (availablePegs == null)
                return;
            if (Warehouse != null)
            {
                availablePegs.Criteria = CriteriaOperator.FromLambda<WarehousePeg>(p => p.Warehouse.Oid == Warehouse.Oid);
                availablePegs.Filter = CriteriaOperator.Parse("WarehouseArea[DeleteOn<?] | !WarehouseArea[].Exists()", CreatedOn, this);
            }
            else
            {
                availablePegs.Criteria = null;
            }
        }

        public override void AfterConstruction()                                                             // установка значения по умолчанию 
        {              
            base.AfterConstruction(); 
            Warehouse = Session.FindObject<Warehouse>(CriteriaOperator.FromLambda<Warehouse>(p => p.WarehouseNumber >= 0));
    
        }

        private XPCollection<AuditDataItemPersistent> auditTrail;



        [CollectionOperationSet(AllowAdd = false, AllowRemove = false)]
        public XPCollection<AuditDataItemPersistent> AuditTrail
        {
            get
            {
                if (auditTrail == null)
                {
                    auditTrail = AuditedObjectWeakReference.GetAuditTrail(Session, this);
                }
                return auditTrail;
            }

        }
    }

    #endregion


    #region Пикеты на складе

    [NavigationItem("WarehouseEditor")]
    [RuleCombinationOfPropertiesIsUnique("f", DefaultContexts.Save, "PegNumber, warehouse")]
    public class WarehousePeg : BaseObject
    {
        public WarehousePeg(Session session) : base(session) {

        }

        private uint? pegNumber = null;
       // [PersistentAlias("Warehouse.warehouseNumber*100 + Warehouse.WarehousPegCount")]                 // 

        [Persistent("PegNumber")]
        public uint? PegNumber                                                                //TODO номер пикета. Будет вычисляться при создании.
        {
            get { return pegNumber; }
            set { SetPropertyValue(nameof(PegNumber), ref pegNumber, value); }

        }

        Warehouse warehouse;                   // ссылка на склад
        [Association("Warehouse-WarehousePegs")]
        public Warehouse Warehouse
        {
            get { return warehouse; }
            set { SetPropertyValue(nameof(Warehouse), ref warehouse, value); }
        }


        [Association("WarehouseArea-WarehousePegs")]
        public XPCollection<WarehouseArea> WarehouseArea
        {
            get { return GetCollection<WarehouseArea>(nameof(WarehouseArea)); }
        }

    }

    #endregion

    #region  Груз на площадке                                             
    [DefaultClassOptions]
    public class CargoOnArea : BaseObject
    {
        public CargoOnArea(Session session) : base(session) { }


        uint weight;                                                                      //Вес груза.
        public uint Weight
        {
            get { return weight; }
            set { SetPropertyValue(nameof(Weight), ref weight, value); }

        }

        DateTime loadedOn;
        public DateTime LoadedOn
        {
            get { return loadedOn; }
            set { SetPropertyValue(nameof(LoadedOn), ref loadedOn, value); }
        }

        DateTime unloadedOn;
        public DateTime UnloadedOn
        {
            get { return unloadedOn; }
            set { SetPropertyValue(nameof(UnloadedOn), ref unloadedOn, value); }
        }

        WarehouseArea warehouseArea;
        [Association("WarehouseArea-CargoOnArea")]
        public WarehouseArea WarehouseArea
        {
            get { return warehouseArea; }


            set
            {
                SetPropertyValue(nameof(WarehouseArea), ref warehouseArea, value);
            }
        }
        #endregion

    }
}



