using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
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
            try
            {
                //обращение к базе данных элементов внутри открытого документа
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;
                //создание экземпляра объекта фильтра
                GroupPickFilter groupPickFilter = new GroupPickFilter();
                //выбор пользователем ссылки на группу объектов
                Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, groupPickFilter, "Выберите группу объектов");
                //получение доступа к самому объекту (Element - родительский класс для всех объектов RevitAPI)
                Element element = doc.GetElement(reference);
                //преобразование объекта к типу Group
                Group group = element as Group;
                //находим центр группы
                XYZ groupCenter = GetElementCenter(group);
                //определение комнаты в которой находится выбранная группа объектов
                Room room = GetRoomByPoint(doc, groupCenter);
                XYZ roomCenter = XYZ.Zero;
                XYZ offset = XYZ.Zero;
                if (room !=null)
                {
                    roomCenter = GetElementCenter(room);    //определение центра комнаты
                    offset = groupCenter - roomCenter;  //определение смещения центра группы относительно центра комнаты
                }
                else
                {
                    TaskDialog.Show("Операция прервана","Выбранная группа объектов не находится в помещении");
                    return Result.Cancelled;
                }
                //выбор точки в помещении куда хотим скопировать объект
                XYZ point = uiDoc.Selection.PickPoint("Выберите точку");
                //получение помещения в котором расположена выбранная точка
                Room insertRoom = GetRoomByPoint(doc, point);
                XYZ insertPoint = XYZ.Zero;
                if (insertRoom != null)
                {
                    XYZ insertRoomCenter = GetElementCenter(insertRoom);    //определение центра помещения
                    insertPoint = insertRoomCenter + offset;    //определение точки вставки с заданным смещением относительно центра помещения
                }
                else
                {
                    insertPoint = point;
                }
                //вставка группы объектов через транзакцию (т.к. вносим изменения в документ)
                Transaction transaction = new Transaction(doc);
                transaction.Start("Копирование группы объектов");
                doc.Create.PlaceGroup(insertPoint, group.GroupType);
                transaction.Commit();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)    //отмена пользователем
            {
                return Result.Cancelled;
            }
            catch (Exception ex)    //остальные ошибки
            {
                message = ex.Message;   //сообщение с описанием ошибки
                return Result.Failed;
            }

            return Result.Succeeded;
        }
        public XYZ GetElementCenter(Element element)    //метод вычисляющий центр элемента
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);    //получаем рамку ограничивающую элемент (Bounding box)
            return (bounding.Max + bounding.Min) / 2;
        }
        public Room GetRoomByPoint(Document doc, XYZ point)   //метод определяющий помещение по выбранной точке
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);    //выбираем все помещия из проекта
            foreach (Element e in collector)
            {
                Room room = e as Room;  //проверка что элемент Room
                if (room != null)
                {
                    if (room.IsPointInRoom(point))  //проверка что точка находится в комнате
                    {
                        return room;
                    }
                }
            }
            return null;
        }
        public class GroupPickFilter : ISelectionFilter //создание фильтра элементов
        {
            public bool AllowElement(Element elem)
            {
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups) //проверка что категория элемента это группа
                    return true;
                else
                    return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }
    }
}
