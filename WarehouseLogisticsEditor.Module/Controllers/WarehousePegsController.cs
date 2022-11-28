using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Layout;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WarehouseLogisticsEditor.Module.BusinessObjects;

namespace WarehouseLogisticsEditor.Module.Controllers
{
    public  class PegsController : ViewController                                      // контроллер автоматической инициализации номера пикета. 
    {
        private NewObjectViewController controller;

        protected override void OnActivated()
        {
            base.OnActivated();
            controller = Frame.GetController<NewObjectViewController>();
            if (controller != null )
            {
                controller.ObjectCreated += controller_ObjectCreated;
            }

        }
        void controller_ObjectCreated (object sender, ObjectCreatedEventArgs e)
        {
            if  (e.CreatedObject is WarehousePeg && ((WarehousePeg)e.CreatedObject).Warehouse!=null)
            {
                WarehousePeg peg = ((WarehousePeg)e.CreatedObject);
                IQueryable<WarehousePeg> pegs = ObjectSpace.GetObjectsQuery<WarehousePeg>();
                 
                int result = pegs.Where(p=> p.Warehouse.Oid == peg.Warehouse.Oid).Count()+ peg.Warehouse.WarehouseNumber*100;


                ((WarehousePeg)e.CreatedObject).PegNumber = (uint?)(++result);
            }
        }
        protected override void OnDeactivated()
        {
            if ( controller != null )
            {
                controller.ObjectCreated -= controller_ObjectCreated;
            }
            base.OnDeactivated();
        }
    }
}
