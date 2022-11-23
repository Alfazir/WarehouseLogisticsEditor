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
        public string WarehouseName                //TODO имя склада (произвольное). Позже наложим ограничения
        {
            get { return warehouseName; }
            set { SetPropertyValue(nameof(WarehouseName), ref warehouseName, value); }
        }

        
        int warehouseNumber;                       //TODO  Номер склада. Позже наложим ограничения

       //[RuleRequiredField(DefaultContexts.Save)]
        public int WarehouseNumber
        {
            get { return warehouseNumber; }
            set { SetPropertyValue(nameof(WarehouseNumber), ref warehouseNumber, value); }
        }

        private int? warehousPegCount = null;      // количество пикетов на складе. Вычисляемое. 
        public int? WarehousPegCount
        {
            get
            {
                if
                (!IsLoading && !IsSaving && warehousPegCount == null)
                    UpdateWarehousPegCount(false); return warehousPegCount;
            }
            // set { SetPropertyValue(nameof(WarehousPegCount), ref WarehousPegCount , value); }
        }
        public void UpdateWarehousPegCount(bool forseChageEvents)  //UNDONE
        {
            int? oldWarehousePegCount = warehousPegCount;
            warehousPegCount = Convert.ToInt32(Evaluate(CriteriaOperator.Parse("WarehousePegs.Count")));
            if (forseChageEvents)
                OnChanged(nameof(WarehousPegCount), oldWarehousePegCount, warehousPegCount);
        }




        [Association("Warehouse-WarehouseAreas")]
        public XPCollection<WarehouseArea> WarehouseAreas                      // коллекция зарегистрированных на складе площадок
        {
            get { return GetCollection<WarehouseArea>(nameof(WarehouseAreas)); }

        }

        [Association("Warehouse-WarehousePegs")] //, Aggregated, ExpandObjectMembers(ExpandObjectMembers.Never)]

        public XPCollection<WarehousePeg> WarehousePegs                      // коллекция зарегистрированных на складе пикетов
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
            // createdOn = DateTime.Now;                             //HACK установка даты (поменять алгоритм вычисления даты)

        }
        string areaName;

        public string AreaName                                    // имя площадки,- поменять на автоматически вычисляемое ( оставить алиас для удобства)
        {
            get { return areaName; }
            set { SetPropertyValue(nameof(AreaName), ref areaName, value); }
        }



        /*  int cargoOnArea;                                          // груз на площадке
          public int CargoOnArea
          {
              get { return cargoOnArea; }
              set { SetPropertyValue(nameof(CargoOnArea), ref cargoOnArea, value); }
          }*/

        [Association("WarehouseArea-CargoOnArea"), Aggregated]
        public XPCollection<CargoOnArea> CargosOnArea                      // коллекция зарегистрированных на складе грузов
        {
            get { return GetCollection<CargoOnArea>(nameof(CargosOnArea)); }
            set { }
        }



        DateTime createdOn; //HACK дата создания площадки.
        // [VisibleInListView(false)]
        [RuleRequiredField(DefaultContexts.Save)]
        public DateTime CreatedOn
        {
            get { return createdOn; }
            set { SetPropertyValue(nameof(CreatedOn), ref createdOn, value); }
        }

        DateTime deleteOn;  //HACK дата удаления площадки.

        [RuleRequiredField(DefaultContexts.Save)]
        public DateTime DeleteOn
        {
            get { return deleteOn; }
            set { SetPropertyValue(nameof(DeleteOn), ref deleteOn, value); }
        }


        Warehouse warehouse;                         // ссылка на склад
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
        //  [RuleValueComparison("RuleWithAggregateFunction", (DefaultContexts)ValueComparisonType.Equals, 0, DefaultContexts.Save, TargetPropertyName = nameof(WrehousePeg.PegNumber), TargetCollectionAggregate= Aggregate.Sum)]
        public XPCollection<WarehousePeg> Pegs
        {
            get
            {

                return GetCollection<WarehousePeg>(nameof(Pegs));
            }
        }


        private XPCollection<WarehousePeg> availablePegs;
        // [Association("WrehouseArea-WrehousePegs")]
        //  [DataSourceProperty("Wrehouse.WrehouseAreas", DataSourcePropertyIsNullMode.SelectAll)]
        [Browsable(false)]
        public XPCollection<WarehousePeg> AvailablePegs                      // коллекция зарегистрированных на складе площадок
        {
            get
            {
                //  XPCollection<WrehousePeg> CollectionPegs = GetCollection<WrehousePeg>(nameof(WrehousePegs));
                //  CollectionPegs.Criteria = new BinaryOperator(nameof(WrehousePeg.Warehouse), this.Warehouse);
                //return CollectionPegs;

                //  return GetCollection<WrehousePeg>(nameof(WrehousePegs));
                if (availablePegs == null)
                {
                    availablePegs = new XPCollection<WarehousePeg>(Session);

                }
                RefreshAvailablePegs();
                return availablePegs;
            }
        }
        private void RefreshAvailablePegs()
        {
            if (availablePegs == null)
                return;
            if (Warehouse != null)
            {


                // availablePegs.Criteria = CriteriaOperator.Parse("[Warehouse] == {8ba50e7a-ea71-41cc-87f1-e8ebfc228c73}");
                //availablePegs.Criteria = CriteriaOperator.Parse($"[Warehouse] == {tt}");

                availablePegs.Criteria = CriteriaOperator.FromLambda<WarehousePeg>(p => p.Warehouse.Oid == Warehouse.Oid);
                availablePegs.Filter = CriteriaOperator.Parse("WarehouseArea[DeleteOn<?] | !WarehouseArea[].Exists()", CreatedOn, this);
                //  availablePegs.Criteria = CriteriaOperator.FromLambda(p=>WrehouseArea)
                //  availablePegs = availablePegs.Where(p=> p.Warehouse = Warehouse.Oid) ;
            }
            else
            {
                availablePegs.Criteria = null;
            }
        }

        public override void AfterConstruction()
        {               // установка значения по умолчанию 
            base.AfterConstruction();
            Warehouse = new Warehouse(Session);
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
    public class WarehousePeg : BaseObject
    {
        public WarehousePeg(Session session) : base(session) { }

        int pegNumber;
        //[PersistentAlias("Warehouse.warehouseNumber* 100 + Warehouse.WarehousPegCount")]                 // 
        public int PegNumber                                   //TODO номер пикета. Будет вычисляться при создании. Определиться с обновлением значения??
        {
            get { return pegNumber; }
            //get { return (int) (EvaluateAlias(nameof (PegNumber)) ?? 0) ; }
            set { SetPropertyValue(nameof(PegNumber), ref pegNumber, value); }
        }

        Warehouse warehouse;                   // ссылка на склад
        [Association("Warehouse-WarehousePegs")]
        public Warehouse Warehouse
        {
            get { return warehouse; }
            set { SetPropertyValue(nameof(Warehouse), ref warehouse, value); }
        }

        //  WrehouseArea wrehouseArea;            // ссылка на площадку
        //[DataSourceProperty("Department.Contacts")]
        [Association("WarehouseArea-WarehousePegs")]
        public XPCollection<WarehouseArea> WarehouseArea
        {

            get { return GetCollection<WarehouseArea>(nameof(WarehouseArea)); }
            //set { SetPropertyValue(nameof(WrehouseArea), ref wrehouseArea, value); }
        }
    }

    #endregion

    #region  Груз на площадке
    public class CargoOnArea : BaseObject
    {
        public CargoOnArea(Session session) : base(session) { }


        uint weight;
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



