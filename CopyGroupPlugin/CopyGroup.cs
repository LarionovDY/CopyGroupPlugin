using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyGroupPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CopyGroup : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //обращение к базе данных элементов внутри открытого документа
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            //выбор пользователем ссылки на группу объектов
            Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, "Выберите группу объектов");
            //получение доступа к самому объекту (Element - родительский класс для всех объектов RevitAPI)
            Element element = doc.GetElement(reference);
            //преобразование объекта к типу Group
            Group group = element as Group;

            //выбор точки в которую хотим скопировать объект
            XYZ point = uiDoc.Selection.PickPoint("Выберите точку");

            //вставка группы объектов через транзакцию (т.к. вносим изменения в документ)
            Transaction transaction = new Transaction(doc);
            transaction.Start("Копирование группы объектов");
            doc.Create.PlaceGroup(point,group.GroupType);
            transaction.Commit();

            return Result.Succeeded;
        }
    }
}
