using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using System;

namespace XafApplication86.Module.Controllers
{
    public class CreateAggregatedObjectController : ViewController
    {

        SimpleAction editAggregatedObjectAction;

        public CreateAggregatedObjectController()
        {
            TargetViewType = ViewType.DetailView;
            TargetViewNesting = Nesting.Nested;
            editAggregatedObjectAction = new SimpleAction(this, "EditAggregatedObject", DevExpress.Persistent.Base.PredefinedCategory.Edit);
            editAggregatedObjectAction.Caption = "Edit";
            editAggregatedObjectAction.ImageName = "Action_Edit";
            editAggregatedObjectAction.Execute += editAggregatedObjectAction_Execute;
        }

        protected override void OnActivated()
        {
            base.OnActivated();

            editAggregatedObjectAction.Enabled["ObjectEmpty"] = View.CurrentObject == null;
            View.CurrentObjectChanged += View_CurrentObjectChanged;

            editAggregatedObjectAction.Active["InDetailPropertyEditor"] = Frame is NestedFrame && ((NestedFrame)Frame).ViewItem is DetailPropertyEditor;

            DetailPropertyEditor detailPropertyEditor = (DetailPropertyEditor)((NestedFrame)Frame).ViewItem;
            editAggregatedObjectAction.Enabled["ParentExists"] = detailPropertyEditor.CurrentObject != null;
            detailPropertyEditor.View.CurrentObjectChanged += ParentView_CurrentObjectChanged;
        }

        void View_CurrentObjectChanged(object sender, EventArgs e)
        {
            editAggregatedObjectAction.Enabled["ObjectEmpty"] = View.CurrentObject == null;
        }

        void ParentView_CurrentObjectChanged(object sender, EventArgs e)
        {
            editAggregatedObjectAction.Enabled["ParentExists"] = ((View)sender).CurrentObject != null;
        }

        void editAggregatedObjectAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            DetailPropertyEditor detailPropertyEditor = (DetailPropertyEditor)((NestedFrame)Frame).ViewItem;
            object masterObject = detailPropertyEditor.CurrentObject;
            detailPropertyEditor.MemberInfo.SetValue(masterObject, ObjectSpace.CreateObject(View.ObjectTypeInfo.Type));
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            View.CurrentObjectChanged -= View_CurrentObjectChanged;
            if (Frame is NestedFrame && ((NestedFrame)Frame).ViewItem is DetailPropertyEditor)
            {
                DetailPropertyEditor detailPropertyEditor = (DetailPropertyEditor)((NestedFrame)Frame).ViewItem;
                detailPropertyEditor.View.CurrentObjectChanged -= ParentView_CurrentObjectChanged;
            }
        }
    }
}