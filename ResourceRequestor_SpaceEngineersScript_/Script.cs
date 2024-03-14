using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VRageMath;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Data;
using Sandbox.Game.GameSystems;
using System.CodeDom;
using System.Collections.ObjectModel;
using Sandbox.Game.Debugging;
using VRage;
using Sandbox.Definitions;
using System.Diagnostics;
using Sandbox.Game;
using VRage.Game.Entity;
using Sandbox.Game.Entities;
using VRage.ObjectBuilders;
//using Sandbox.ModAPI;
//using VRage.Game.ModAPI;


namespace Template
{

    public sealed class Program : MyGridProgram
    {
        #region Copy

        /** InputData - статический класс, хранящий входные значения скрипта. Здесь необходимо менять названия блоков
         * 
         */
        static class InputData
        {
            // Имя панели ввода
            static public string InputPanelName { get; private set; } = "Input Panel 1";

            // Имя панели вывода
            static public string OutputPanelName { get; private set; } = "Output Panel 1";

            //  Имя отправного контейнера
            static public string StartingContainerName { get; private set; } = "Starting Container 1";

            // Имя конечного контейнера
            static public string DestinationContainerName { get; private set; } = "Destination Container 1";

            // Имя сборщика
            static public string AssemblerName { get; private set; } = "Private Assembler 1";
        }

        class TransferItem
        {
            public string SubtypeId { get; private set; }

            public int RequestedAmount { get; private set; }

            public bool IsTransfered { get; set; }


            public MyInventoryItem Item { get; private set; }

            public bool HasItem { get; private set; } = false;

            public bool IsClear { get; private set; } = true;

            public bool IsRequested { get; set; } = false;

            public TransferItem()
            {
                SubtypeId = "";
                RequestedAmount = -1;
                IsTransfered = false;

                Item = new MyInventoryItem();
                HasItem = false;
                IsClear = true;

                IsRequested = false;
            }

            public bool setData(string subtypeId, int amount, bool isTransfered, MyInventoryItem item, bool isRequested)
            {

                if (this.IsTransfered || item == new MyInventoryItem())
                {
                    return false;
                }

                SubtypeId = subtypeId;
                RequestedAmount = amount;
                IsTransfered = isTransfered;

                Item = item;
                HasItem = true;
                IsClear = false;

                IsRequested = isRequested;

                return true;
            }

            public bool setData(string subtypeId, int amount, bool isTransfered, MyInventoryItem item)
            {

                if(this.IsTransfered || item == new MyInventoryItem())
                {
                    return false;
                }

                SubtypeId = subtypeId;
                RequestedAmount = amount;
                IsTransfered = isTransfered;

                Item = item;
                HasItem = true;
                IsClear = false;

                IsRequested = false;

                return true;
            }

            public bool setData(string subtypeId, int amount, bool isTransfered)
            {
                SubtypeId = subtypeId;
                RequestedAmount = amount;
                IsTransfered = isTransfered;

                Item = new MyInventoryItem();
                HasItem = false;
                IsClear = false;

                IsRequested = false;

                return true;
            }

            public void clear()
            {
                SubtypeId = "";
                RequestedAmount = -1;
                IsTransfered = false;

                Item = new MyInventoryItem();
                HasItem = false;
                IsClear = true;

                IsRequested = false;
            }


        }

        static class Worker
        {

            // Грид система терминала
            public static IMyGridTerminalSystem GridTerminalSystem { get; set; }

            // Состояние работы
            public static WorkStates actualWorkState { get; set; } = Worker.WorkStates.WaitingStart;

            public static void resetWorkState()
            {
                Worker.actualWorkState = Worker.WorkStates.WaitingStart;
            }

            // Методы выполнения работы ( перемещение ресурсов по запросу ) 
            public static void work()
            {

                // Устанавливаем флаг, что мы начали работу
                Worker.actualWorkState = WorkStates.Processing;

                // Берем отправной инвентарь
                IMyInventory startingInventory = Worker.GridTerminalSystem.GetBlockWithName(InputData.StartingContainerName).GetInventory();
                // Берем инвентарь назначения
                IMyInventory destinationInventory = Worker.GridTerminalSystem.GetBlockWithName(InputData.DestinationContainerName).GetInventory();

                // Инициализируем парсер
                InputPanelTextParser parser = new InputPanelTextParser();

                // Инициализируем словарь типа "подтип-количество"
                List<TransferItem> transferItems = new List<TransferItem>();

                // Если парсер распарсил данные панели ввода в словарь
                if (parser.parseInputPanelText(PanelWriter.InputPanel, transferItems))
                {

                    // Инициализируем и заполняем список предметов инвентаря
                    List<MyInventoryItem> items = new List<MyInventoryItem>();
                    startingInventory.GetItems(items);

                    // Заполняем словарь
                    TransferItemUtils.coordinateTransferItems(transferItems, items);


                    // Инициализируем переносчик
                    SmartItemTransferer smartTransferer = new SmartItemTransferer();

                    // Инициализируем менеджер сборщика
                    AssemblerManager asmManager = new AssemblerManager(Worker.GridTerminalSystem.GetBlockWithName(InputData.AssemblerName) as IMyAssembler);

                    // Переносим запрошенные ресурсы по запрошенному адресу
                    if (smartTransferer.smartTransferTo(startingInventory, destinationInventory, transferItems, asmManager))
                    {

                        // Устанавливаем флаг о завершении работы
                        Worker.actualWorkState = Worker.WorkStates.Completed;
                    }

                    else
                    {
                        // Если работа не приостановлена
                        if (Worker.actualWorkState != Worker.WorkStates.WaitingResources)
                        {
                            // То она оборвана, значит устанавливаем соответствующий флаг
                            Worker.actualWorkState = Worker.WorkStates.Aborted;
                        }
                    }

                }
            }

            // Метод возобновления работы
            public static void workResumption()
            {

                // Очищаем список предметов на перенос от тех, которые уже перенесены
                TransferItemUtils.removeTransferedItems(SmartItemTransfererSnapshot.Snapshot.TransferItems);

                // Берем отправной инвентарь
                IMyInventory startingInventory = Worker.GridTerminalSystem.GetBlockWithName(InputData.StartingContainerName).GetInventory();
                // Берем инвентарь назначения
                IMyInventory destinationInventory = Worker.GridTerminalSystem.GetBlockWithName(InputData.DestinationContainerName).GetInventory();

                // Устанавливаем флаг, что работа снова в процесса
                Worker.actualWorkState = Worker.WorkStates.Processing;

                // Вынимаем из снимка умный переносчик предметов
                SmartItemTransferer smartTransferer = SmartItemTransfererSnapshot.Transferer;


                // Обновляем данные о предметах инвентаря
                List<TransferItem> transferItems = SmartItemTransfererSnapshot.Snapshot.TransferItems;
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                startingInventory.GetItems(items);

                TransferItemUtils.coordinateTransferItems(transferItems, items);


                // Продолжаем умный перенос предметов, с обновленными инвентарями
                if (smartTransferer.smartTransferTo(startingInventory,
                                                destinationInventory,
                                                SmartItemTransfererSnapshot.Snapshot.TransferItems,
                                                SmartItemTransfererSnapshot.Snapshot.AsmManager))
                {

                    // Устанавливаем флаг о завершении работы
                    Worker.actualWorkState = Worker.WorkStates.Completed;
                }

                else
                {
                    // Если работа не приостановлена
                    if (Worker.actualWorkState != Worker.WorkStates.WaitingResources)
                    {
                        // То она оборвана, значит устанавливаем соответствующий флаг
                        Worker.actualWorkState = Worker.WorkStates.Aborted;
                    }
                }
            }


            // Набор состояний
            public enum WorkStates : int
            {
                // В ожидании начала работы
                WaitingStart,

                // В процессе работы
                Processing,

                // В ожидании ресурсов
                WaitingResources,

                // Работа завершена
                Completed,

                // Работа некорректно прервана
                Aborted
            }

        }


        // Класс, содержащий utils данные и методы для панели ввода
        static class InputPanelTextHelper
        {

            // Список подтипов компонентов
            public static string[] ComponentSubtypes { get; private set; } = new string[21]
            {
            "BulletproofGlass", "Computer", "Construction", "Detector", "Display", "Explosives",
            "Girder", "GravityGenerator", "InteriorPlate", "LargeTube", "Medical", "MetalGrid", "Motor",
            "PowerCell", "RadioCommunication", "Reactor", "SmallTube", "SolarCell", "SteelPlate", "Superconductor",
            "Thrust"
            };

            // Список имён компонентов на русском языке
            public static string[] ComponentNamesRU { get; private set; } = new string[21]
            {
            "Бронированноестекло", "Компьютер", "Строительныекомпоненты", "Компонентыдетектора", "Экран", "Взрывчатка",
            "Балка", "Компонентыгравитационногогенератора", "Внутренняяпластина", "Большаястальнаятруба", "Медицинскиекомпоненты", "Компонентрешётки", "Мотор",
            "Энергоячейка", "Радиокомпоненты", "Компонентыреактора", "Малаятрубка", "Солнечнаяячейка", "Стальнаяпластина", "Сверхпроводник",
            "Деталиионногоускорителя"
            };

            // Словарь имя компонента на русском - подтип компонента
            public static Dictionary<string, string> ComponentNamesRUSubtypesENG { get; private set; } = new Dictionary<string, string>()
            {
            { "Бронированноестекло", "BulletproofGlass" }, { "Компьютер",  "Computer" }, { "Строительныекомпоненты", "Construction" },
            { "Компонентыдетектора", "Detector" }, { "Экран", "Display" }, { "Взрывчатка", "Explosives" },
            { "Балка", "Girder" }, { "Компонентыгравитационногогенератора", "GravityGenerator" }, { "Внутренняяпластина", "InteriorPlate" },
            { "Большаястальнаятруба", "LargeTube" }, { "Медицинскиекомпоненты", "Medical" }, { "Компонентрешётки", "MetalGrid" },
            { "Мотор", "Motor" }, { "Энергоячейка", "PowerCell" }, { "Радиокомпоненты", "RadioCommunication" },
            { "Компонентыреактора", "Reactor" }, { "Малаятрубка", "SmallTube" }, { "Солнечнаяячейка", "SolarCell" },
            { "Стальнаяпластина", "SteelPlate" }, { "Сверхпроводник", "Superconductor" }, { "Деталиионногоускорителя", "Thrust" }
            };

            // Заглавие текста по умолчанию
            public static string DefaultTextTitle { get; private set; } = "Список запрашиваемых компонентов: ";

            // Метод установки стандартного вида дисплея
            public static void setDefaultSurfaceView(IMyTextPanel panel)
            {
                panel.BackgroundColor = Color.Black;
                panel.FontColor = Color.Yellow;
                panel.FontSize = 0.7f;
            }

            // Метод записи стандартного текста в панель ввода
            public static void writeDefaultText(IMyTextPanel inputPanel)
            {
                inputPanel.WriteText(DefaultTextTitle + '\n', false);

                foreach (string componentNameRU in ComponentNamesRU)
                {
                    inputPanel.WriteText(componentNameRU + " = 0" + '\n', true);
                }
            }

            // Метод проверки строки на соответствие заглавию текста по умолчанию
            public static bool isDefaultText(string text)
            {
                return text == DefaultTextTitle;
            }
        }

        // Класс-парсер данных компонентов из панели ввода
        class InputPanelTextParser
        {
            // Необходимый размер данных
            public const int requiredDataStringsSize = 21;

            // Метод список предметов на перенос
            public bool parseInputPanelText(IMyTextPanel inputPanel, List<TransferItem> transferItems)
            {
                // Очищаем заполняемый словарь
                transferItems.Clear();

                // Инициализируем и заполняем динамическую строку текстом из панели ввода
                StringBuilder tempBuilder = new StringBuilder();
                inputPanel.ReadText(tempBuilder);

                // Проверка на пустоту и содержание символа перехода на следующую строку
                if (tempBuilder.ToString() == "" || !tempBuilder.ToString().Contains('\n'))
                {
                    Worker.actualWorkState =  Worker.WorkStates.Aborted;
                    return false;
                }

                // Разбиваем динамическую строку на список неизменяемых строк
                List<string> inputPanelDataStrings = tempBuilder.ToString().Split('\n').ToList<string>();

                // Если первая строка в списке - не заглавие
                if (!InputPanelTextHelper.isDefaultText(inputPanelDataStrings[0]))
                {
                    Worker.actualWorkState = Worker.WorkStates.Aborted;
                    return false;
                }

                // Удаляем последний лишний перенос строки
                inputPanelDataStrings.Remove(inputPanelDataStrings[inputPanelDataStrings.Count - 1]);

                // Удаляем заглавие
                inputPanelDataStrings.Remove(inputPanelDataStrings.First());

                // Если размер сформированного списка не равен заданному
                if (inputPanelDataStrings.Count != requiredDataStringsSize)
                {
                    Worker.actualWorkState = Worker.WorkStates.Aborted;
                    return false;
                }

                // Проходим по каждой строке компонентов
                foreach (string componentString in inputPanelDataStrings)
                {

                    // Если строка данных компонента не содержит пробел или символ '='
                    if (!componentString.Contains(' ') || !componentString.Contains('='))
                    {
                        // Очищаем словарь т.к. в него уже могли добавится данные, без очистки словаря при обрыве его заполнения в нём останется мусор
                        transferItems.Clear();

                        PanelWriter.writeOutputDataLine("Парсер: зашли в не соедржит ' ' или = ", true);
                        PanelWriter.writeOutputDataLine(componentString, true);

                        Worker.actualWorkState = Worker.WorkStates.Aborted;

                        return false;
                    }


                    // Очищаем строку от пробелов
                    string newComponentString = componentString.Replace(" ", "");

                    // Разбиваем данные компонента по символу '='
                    string[] componentNameAmount = newComponentString.Split('=');



                    // Проверка на число
                    foreach (char ch in componentNameAmount[1])
                    {
                        // Если в строке, где должно быть количество запрошенных ресурсов, присутствует не цифра
                        if (!Char.IsDigit(ch))
                        {

                            // Откидываем лог для оператора
                            PanelWriter.writeOutputDataLine("Ошибка! В количество компонента передано не число", true);

                            // Переводим флаг в прерывание
                            Worker.actualWorkState = Worker.WorkStates.Aborted;

                            return false;
                        }
                    }

                    int amount = Int16.Parse(componentNameAmount[1]);

                    // Добавляем в словарь подтип компонента и количество
                    if (InputPanelTextHelper.ComponentNamesRUSubtypesENG.Keys.Contains(componentNameAmount[0]) && amount > 0)
                    {
                        TransferItem transferItem = new TransferItem();
                       
                        transferItem.setData(InputPanelTextHelper.ComponentNamesRUSubtypesENG[componentNameAmount[0]], amount, false);

                        PanelWriter.writeOutputDataLine("Мы добавили предмет " + transferItem.SubtypeId + " в transferItems ", true);

                        transferItems.Add(transferItem);

                        PanelWriter.writeOutputDataLine("Вывод traferItems", true);
                        foreach (TransferItem item in transferItems)
                        {
                            PanelWriter.writeOutputDataLine(item.SubtypeId, true);
                        }

                        PanelWriter.writeOutputDataLine("Предмет содержится в списке?" + transferItems.Contains(transferItem).ToString(), true);
                    }

                }

                if(transferItems.Count < 1)
                {
                    PanelWriter.writeOutputDataLine("Перенос прерван, ни один предмет не был запрошен", true);
                    Worker.actualWorkState = Worker.WorkStates.Aborted;
                    return false;
                }

                return true;
            }
        }


        static class TransferItemUtils
        {


            // Метод для совмещения предметов на перенос и предметов, находящихся в данный момент в инвентаре
            public static bool coordinateTransferItems(List<TransferItem> transferItems, List<MyInventoryItem> items)
            {

                // Если предметы на перенос пусты
                if (transferItems.Count < 1)
                {
                    PanelWriter.writeOutputDataLine("coordinateTransferItems: мы зашли в неудачную проверку на кол-во содержимого", true);
                    return false;
                }
                
                // Проходим по всем предметам для переноса
                foreach (TransferItem transferItem in transferItems)
                {

                    // Если предмет для переноса уже перенесен или очищен или заполнен
                    if (transferItem.IsTransfered || transferItem.IsClear)
                    {
                        PanelWriter.writeOutputDataLine("coordinateTransferItems: мы зашли в неудачную проверку флагов", true);
                        return false;
                    }



                    // Проходим по всем предметам из инвентаря
                    foreach (MyInventoryItem item in items)
                    {

                        // Если подтип у предмета для переноса и у предмета из инвентаря совпадает
                        if (item.Type.SubtypeId == transferItem.SubtypeId)
                        {

                            PanelWriter.writeOutputDataLine("coordinateTransferItems: мы меняем данные объекту " + transferItem.SubtypeId, true);

                            // Включаем в предмет для переноса предмет из инвентаря
                            if(transferItem.setData(transferItem.SubtypeId, transferItem.RequestedAmount, transferItem.IsTransfered, item, transferItem.IsRequested))
                            {
                                PanelWriter.writeOutputDataLine("coordinateTransferItems: мы поменяли данные  объекту " + transferItem.SubtypeId, true);
                                PanelWriter.writeOutputDataLine("coordinateTransferItems: ЕГО КАЛВО" + transferItem.Item.Amount, true);
                            }
                            else
                            {
                                PanelWriter.writeOutputDataLine("coordinateTransferItems: мы НЕ поменяли данные  объекту " + transferItem.SubtypeId, true);
                            }
                           
                            
                        }

                    }



                }

                return true;
            }
        
            public static bool removeTransferedItems(List<TransferItem> transferItems)
            {
                PanelWriter.writeOutputDataLine("removeTransferedItems: вывод transferitems:", true);
                foreach (var item in transferItems)
                {
                    PanelWriter.writeOutputDataLine(item.SubtypeId, true);
                }

                // Идем по порядку с конца списка и удаляем необходимые предметы
                for (int i = transferItems.Count - 1; i >= 0; i--)
                {

                    if (transferItems[i].IsTransfered)
                    {
                        PanelWriter.writeOutputDataLine("removeTransferedItems: удаляем предмет: " + transferItems[i].SubtypeId, true);
                        transferItems.Remove(transferItems[i]);
                    }

                }

                return true;

            }
        }

        

        class AssemblerManager
        {
            // Сборщик
            public IMyAssembler Assembler { get; set; }

            public string BlueprintType { get; private set; } = "MyObjectBuilder_BlueprintDefinition/";

            public Dictionary<string, string> ComponentAndBlueprintSubtypes { get; private set; } = new Dictionary<string, string>()
            {
            { "BulletproofGlass", "BulletproofGlass" }, { "Computer",  "ComputerComponent" }, { "Construction", "ConstructionComponent" },
            { "Detector", "DetectorComponent" }, { "Display", "Display" }, { "Explosives", "ExplosivesComponent" },
            { "Girder", "GirderComponent" }, { "GravityGenerator", "GravityGeneratorComponent" }, { "InteriorPlate", "InteriorPlate" },
            { "LargeTube", "LargeTube" }, { "Medical", "MedicalComponent" }, { "MetalGrid", "MetalGrid" },
            { "Motor", "MotorComponent" }, { "PowerCell", "PowerCell" }, { "RadioCommunication", "RadioCommunicationComponent" },
            { "Reactor", "ReactorComponent" }, { "SmallTube", "SmallTube" }, { "SolarCell", "SolarCell" },
            { "SteelPlate", "SteelPlate" }, { "Superconductor", "Superconductor" }, { "Thrust", "ThrustComponent" }
            };

            // Конструктор по умолчанию
            public AssemblerManager(IMyAssembler assembler)
            {
                Assembler = assembler;
            }

            public bool assembleComponent(string subtypeId, int amount)
            {

                if(!ComponentAndBlueprintSubtypes.Keys.Contains(subtypeId))
                {
                    PanelWriter.writeOutputDataLine("Компонент с подтипом: " + subtypeId, true);
                    PanelWriter.writeOutputDataLine("не содержится в словаре ComponentAndBlueprintSubtypes.", true);

                    return false;
                }

                // Получаем MyDefinitionId из item
                MyDefinitionId defID = MyDefinitionId.Parse(this.BlueprintType + this.ComponentAndBlueprintSubtypes[subtypeId]);

                // Получаем MyFixedPoint из amount
                MyFixedPoint fpAmount = amount;

                PanelWriter.writeOutputDataLine("defID: " + defID , true);

                PanelWriter.writeOutputDataLine((this.Assembler.CanUseBlueprint(defID)).ToString(), true);

                // Добавляем в очередь создание предмета item в количестве amount
                this.Assembler.AddQueueItem(defID, fpAmount);

                PanelWriter.writeOutputDataLine("ПОСЛЕ адд ту квае", true);


                return true;
            }

        }

        // Глобальный класс снимка умного переносчика ресурсов
        static class SmartItemTransfererSnapshot
        {

            // Переносчик предметов
            public static SmartItemTransferer Transferer { get; private set; }

            // Снимок метода smartTransferTo
            public static SmartTransferToSnapshot Snapshot { get; private set; }

            public static void saveSnapshot(SmartItemTransferer transferer, SmartTransferToSnapshot snapshot)
            {
                SmartItemTransfererSnapshot.Transferer = transferer;
                SmartItemTransfererSnapshot.Snapshot = snapshot;
            }

            // Класс-снимок метода smartTransferTo класса ItemTransferer
            public class SmartTransferToSnapshot
            {

                // Отправной инвентарь
                public IMyInventory StartingInventory { get; private set; }

                // Инвентарь назначения
                public IMyInventory DestinationInventory { get; private set; }

                // Список предметов на перенос
                public List<TransferItem> TransferItems { get; private set; }

                // Менеджер сборщика
                public AssemblerManager AsmManager { get; private set; }

                // Конструктор по умолчанию
                public SmartTransferToSnapshot(IMyInventory startingInventory, IMyInventory destinationInventory, List<TransferItem> transferItems, AssemblerManager asmManager)
                {
                    this.StartingInventory = startingInventory;
                    this.DestinationInventory = destinationInventory;
                    this.TransferItems = transferItems;
                    this.AsmManager = asmManager;
                }
            }
        }

        // Класс перемещения предметов из одного инвентаря в другой инвентарь
        class SmartItemTransferer
        {

            // Метод перемещения из одного инвентаря в другой с использованием 2-ух словарей: словарь типа "подтип-предмет" и словарь типа "подтип-количество"
            public bool smartTransferTo(IMyInventory startingInventory, IMyInventory destinationInventory, List<TransferItem> transferItems, AssemblerManager asmManager)
            {

                // Если аргументы некорректны
                if (startingInventory == null || destinationInventory == null || transferItems.Count < 1 || asmManager == null)
                {
                    Worker.actualWorkState = Worker.WorkStates.Aborted;
                    return false;
                }

                // Проходим по всему словарю
                foreach (TransferItem transferItem in transferItems)
                {

                    // Если данные предмета для переноса некорректны
                    if (transferItem.IsTransfered || transferItem.IsClear)
                    {
                        PanelWriter.writeOutputDataLine("Некорректные данные предмета для переноса", true);
                        Worker.actualWorkState = Worker.WorkStates.Aborted;
                        return false;
                    }

                    /// DEBUG START
                    PanelWriter.writeOutputDataLine("В наличии предметов: " + transferItem.Item.Amount, true);
                    PanelWriter.writeOutputDataLine("Запрошенно предметов: " + transferItem.RequestedAmount, true);
                    /// DEBUG END

                    PanelWriter.writeOutputDataLine("smartTransferTo: transferItem.Item.Type:" + transferItem.Item.Type, true);

                    // Если в инвентаре недостаточно предметов необходимого типа
                    if (transferItem.Item.Amount < transferItem.RequestedAmount)
                    {

                        // Если глобальный флаг работы - приостановлена, то сразу же возвращаемся назад, ибо мы
                        // ничего не должны сделать, ни новый запрос ресурсов, ни изменение снимка
                        if (Worker.actualWorkState != Worker.WorkStates.WaitingResources && Worker.actualWorkState != Worker.WorkStates.Processing)
                        {
                            PanelWriter.writeOutputDataLine("smartTransferTo: некорректное состояние работы", true);
                            Worker.actualWorkState = Worker.WorkStates.Aborted;
                            return false;
                        }

                        if (!transferItem.IsRequested)
                        {
                            // Ставим флаг, что предмет заказан
                            transferItem.IsRequested = true;

                            // Отправляем сборщику запрос на необходимые предметы
                            if (!asmManager.assembleComponent(transferItem.SubtypeId, transferItem.RequestedAmount))
                            {
                                Worker.actualWorkState = Worker.WorkStates.Aborted;
                                return false;
                            }

                        }

                        // Сохраняем глобальный снимок себя
                        SmartItemTransfererSnapshot.saveSnapshot(this, new SmartItemTransfererSnapshot.SmartTransferToSnapshot(startingInventory, destinationInventory, transferItems, asmManager));

                        // Устанавливаем приостановленный статус работе
                        Worker.actualWorkState = Worker.WorkStates.WaitingResources;

                        // Возвращаем ответ о том, что мы не перенесли предметы ( просто не закончили )
                        return false;


                    }


                    // Перекидываем предметы в запрошенном количестве в пункт назначения
                    if (startingInventory.TransferItemTo(destinationInventory, transferItem.Item, transferItem.RequestedAmount))
                    {

                        // Ставим флаг предмету, что он перенесен
                        transferItem.IsTransfered = true;
                        
                        PanelWriter.writeOutputDataLine("Предмет " + transferItem.SubtypeId + " перенесен по назначению", true);
                        

                    }

                    else
                    {
                        
                        PanelWriter.writeOutputDataLine("Предмет " + transferItem.SubtypeId + " не перенесен по назначению, TransferItemTo вернуло false", true);
                        
                        
                        return false;
                    }
                }

                /// DEBUG START
                PanelWriter.writeOutputDataLine("Окончание работы переноса предметов.", true);
                /// DEBUG END

                return true;
            }
        }


        // Глобальный класс для записи в дисплеи
        static class PanelWriter
        {

            public static IMyTextPanel InputPanel { get; set; }

            public static IMyTextPanel OutputPanel { get; set; }

            public static void writeInputData(string text, bool append)
            {
                InputPanel.WriteText(text, append);
            }

            public static void writeInputData(StringBuilder text, bool append)
            {
                InputPanel.WriteText(text, append);
            }

            public static void writeOutputData(string text, bool append)
            {
                OutputPanel.WriteText(text, append);
            }

            public static void writeOutputData(StringBuilder text, bool append)
            {
                OutputPanel.WriteText(text, append);
            }

            public static void writeOutputDataLine(string text, bool append)
            {
                OutputPanel.WriteText(text + '\n', append);
            }

            public static void writeOutputDataLine(StringBuilder text, bool append)
            {
                OutputPanel.WriteText(text.Append('\n'), append);
            }

        }



        public Program()
        {

            // Берем панель ввода
            IMyTextPanel inputPanel = GridTerminalSystem.GetBlockWithName(InputData.InputPanelName) as IMyTextPanel;
            // Берем панель вывода
            IMyTextPanel outputPanel = GridTerminalSystem.GetBlockWithName(InputData.OutputPanelName) as IMyTextPanel;

            // 
            PanelWriter.InputPanel = inputPanel;
            PanelWriter.OutputPanel = outputPanel;

            PanelWriter.writeOutputData("", false);

            // Устанавливаем панели вывода стандартный вид
            InputPanelTextHelper.setDefaultSurfaceView(outputPanel);

            // Устанавливаем панели ввода стандартный вид и вводим исходные данные
            InputPanelTextHelper.setDefaultSurfaceView(inputPanel);
            InputPanelTextHelper.writeDefaultText(inputPanel);

            //
            Worker.GridTerminalSystem = GridTerminalSystem;

            // Устанавливаем частоту тиков скрипта на раз в ~1.5 секунды
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {

            StringBuilder DEBUGSTR_1 = new StringBuilder(), DEBUGSTR_2 = new StringBuilder(), DEBUGSTR_3 = new StringBuilder();

            switch (Worker.actualWorkState)
            {
                case Worker.WorkStates.WaitingStart:

                    // Если передали аргумент "start", то начинаем работу
                    if (argument == "start")
                    {

                        PanelWriter.writeOutputDataLine("Мы начали работу", false);

                        Worker.work();
                    }

                    break;

                case Worker.WorkStates.Processing:


                    PanelWriter.writeOutputDataLine("Мы в работе", true);

                    // Ничего не делаем

                    break;

                case Worker.WorkStates.WaitingResources:

                    PanelWriter.writeOutputDataLine("Мы ожидаем ресурсы", true);

                    // Пытаемся восстановить работу
                    Worker.workResumption();

                    break;

                case Worker.WorkStates.Completed:

                    // Тута выводим лог о завершении работы
                    PanelWriter.writeOutputDataLine("Перенос предметов завершен!", true);


                    // Устанавливаем стандартные значения после таймаута
                    // TODO: вынести это и это же из Program() в класс
                    IMyTextPanel inputPanel = GridTerminalSystem.GetBlockWithName(InputData.InputPanelName) as IMyTextPanel;
                    IMyTextPanel outputPanel = GridTerminalSystem.GetBlockWithName(InputData.OutputPanelName) as IMyTextPanel;


                    InputPanelTextHelper.setDefaultSurfaceView(outputPanel);
                    InputPanelTextHelper.setDefaultSurfaceView(inputPanel);
                    InputPanelTextHelper.writeDefaultText(inputPanel);

                    Worker.resetWorkState();

                    break;

                case Worker.WorkStates.Aborted:

                    // Тута выводим лог о завершении работы
                    PanelWriter.writeOutputDataLine("Перенос прерван!", true);


                    // Устанавливаем стандартные значения после таймаута
                    // TODO: вынести это и это же из Program() в класс
                    IMyTextPanel inputP = GridTerminalSystem.GetBlockWithName(InputData.InputPanelName) as IMyTextPanel;
                    IMyTextPanel outputP = GridTerminalSystem.GetBlockWithName(InputData.OutputPanelName) as IMyTextPanel;


                    InputPanelTextHelper.setDefaultSurfaceView(outputP);
                    InputPanelTextHelper.setDefaultSurfaceView(inputP);
                    InputPanelTextHelper.writeDefaultText(inputP);

                    Worker.resetWorkState();

                    break;

                default:
                    break;

            }
        }
        public void Save()
        {

        }
        #endregion
    }
}
