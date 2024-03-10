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

        static class Worker
        {

            // Грид система терминала
            public static IMyGridTerminalSystem GridTerminalSystem { get; set; }

            // Состояние работы
            public static WorkStates actualWorkState { get; set; } = Worker.WorkStates.Waiting;



            // Методы выполнения работы ( перемещение ресурсов по запросу ) 
            public static void work(StringBuilder DEBUGSTR_1, StringBuilder DEBUGSTR_2, StringBuilder DEBUGSTR_3)
            {

                // Устанавливаем флаг, что мы начали работу
                Worker.actualWorkState = WorkStates.InProgress;


                // Берем отправной инвентарь
                IMyInventory startingInventory = Worker.GridTerminalSystem.GetBlockWithName(InputData.StartingContainerName).GetInventory();
                // Берем инвентарь назначения
                IMyInventory destinationInventory = Worker.GridTerminalSystem.GetBlockWithName(InputData.DestinationContainerName).GetInventory();

                // Берем панель ввода
                IMyTextPanel inputPanel = Worker.GridTerminalSystem.GetBlockWithName(InputData.InputPanelName) as IMyTextPanel;
                // Берем панель вывода
                IMyTextPanel outputPanel = Worker.GridTerminalSystem.GetBlockWithName(InputData.OutputPanelName) as IMyTextPanel;

                // Записываем первую строку в панель ввода
                outputPanel.WriteText("Запрошенные ресурсы из панели ввода: " + '\n', false);

                // Инициализируем парсер
                InputPanelTextParser parser = new InputPanelTextParser();

                // Инициализируем словарь типа "подтип-количество"
                Dictionary<string, int> subtypesAmounts = new Dictionary<string, int>();

                // Если парсер распарсил данные панели ввода в словарь
                if (parser.parseInputPanelText(inputPanel, subtypesAmounts, DEBUGSTR_1))
                {

                    // Инициализируем словарь типа подтип предмета - предмет
                    Dictionary<string, MyInventoryItem> subtypesItems = new Dictionary<string, MyInventoryItem>();

                    // Инициализируем и заполняем список предметов инвентаря
                    List<MyInventoryItem> items = new List<MyInventoryItem>();
                    startingInventory.GetItems(items);

                    // Заполняем словарь
                    ItemDictionaryFiller.fillSubtypeIdItemDictionary(subtypesItems, InputPanelTextHelper.ComponentSubtypes, items);

                    // Инициализируем переносчик
                    SmartItemTransferer smartTransferer = new SmartItemTransferer();

                    // Инициализируем менеджер сборщика
                    AssemblerManager asmManager = new AssemblerManager(Worker.GridTerminalSystem.GetBlockWithName(InputData.AssemblerName) as IMyAssembler);

                    // Переносим запрошенные ресурсы по запрошенному адресу
                    if (smartTransferer.smartTransferTo(startingInventory, destinationInventory, subtypesItems, subtypesAmounts, asmManager, DEBUGSTR_2))
                    {
                        DEBUGSTR_3.AppendLine("Мы переместили предметы!");

                        // Устанавливаем флаг о завершении работы
                        Worker.actualWorkState = Worker.WorkStates.Completed;
                    }
                    else
                    {
                        DEBUGSTR_3.AppendLine("Перемещение предметов не удалось!");
                    }

                }
            }

            // Метод возобновления работы
            public static void workResumption(StringBuilder DEBUGSTR_1, StringBuilder DEBUGSTR_2, StringBuilder DEBUGSTR_3)
            {

                // Устанавливаем флаг, что работа снова в процесса
                Worker.actualWorkState = Worker.WorkStates.InProgress;

                // Вынимаем из снимка умный переносчик предметов
                SmartItemTransferer smartTransferer = SmartItemTransfererSnapshot.Transferer;

                // Продолжаем умный перенос предметов
                if(smartTransferer.smartTransferTo(SmartItemTransfererSnapshot.Snapshot.StartingInventory,
                                                SmartItemTransfererSnapshot.Snapshot.DestinationInventory,
                                                SmartItemTransfererSnapshot.Snapshot.SubtypesItems,
                                                SmartItemTransfererSnapshot.Snapshot.SubtypesAmounts,
                                                SmartItemTransfererSnapshot.Snapshot.AsmManager,
                                                SmartItemTransfererSnapshot.Snapshot.DEBUGSTR))
                {
                    DEBUGSTR_3.AppendLine("Мы переместили предметы!");

                    // Устанавливаем флаг о завершении работы
                    Worker.actualWorkState = Worker.WorkStates.Completed;
                }
                else
                {
                    DEBUGSTR_3.AppendLine("Перемещение предметов не удалось!");
                }
            }


            // Набор состояний
            public enum WorkStates : int
            {
                // В ожидании начала работы
                Waiting,

                // В процессе работы
                InProgress,

                // Работа приостановлена
                Paused,

                // Работа завершена
                Completed
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

                // Словарь типа "подтип-предмет"
                public Dictionary<string, MyInventoryItem> SubtypesItems { get; private set; }

                // Словарь типа "подтип-количество"
                public Dictionary<string, int> SubtypesAmounts { get; }

                // Менеджер сборщика
                public AssemblerManager AsmManager { get; }

                ///
                /// DEBUGSTR
                public StringBuilder DEBUGSTR { get; }
                ///

                // Конструктор по умолчанию
                public SmartTransferToSnapshot(IMyInventory startingInventory, IMyInventory destinationInventory, Dictionary<string, MyInventoryItem> subtypesItems, Dictionary<string, int> subtypesAmounts, AssemblerManager asmManager, StringBuilder DEBUGSTR)
                {
                    this.StartingInventory = startingInventory;
                    this.DestinationInventory = destinationInventory;
                    this.SubtypesItems = subtypesItems;
                    this.SubtypesAmounts = subtypesAmounts;
                    this.AsmManager = asmManager;
                    this.DEBUGSTR = DEBUGSTR;
                }
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
            "Балка", "Компонентыгравитационногогенератора", "Внутренняя пластина", "Большаястальнаятруба", "Медицинскиекомпоненты", "Компонентрешётки", "Мотор",
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
            public const int requiredDataStringsSize = 22;

            // Метод формирующий словарь типа "подтип-количество" из данных панели ввода
            public bool parseInputPanelText(IMyTextPanel inputPanel, Dictionary<string, int> fillableDict, StringBuilder DEBUGSTR)
            {
                // Очищаем заполняемый словарь
                fillableDict.Clear();

                // Инициализируем и заполняем динамическую строку текстом из панели ввода
                StringBuilder tempBuilder = new StringBuilder();
                inputPanel.ReadText(tempBuilder);

                // Проверка на пустоту и содержание символа перехода на следующую строку
                if (tempBuilder.ToString() == "" || !tempBuilder.ToString().Contains('\n'))
                {
                    return false;
                }

                // Разбиваем динамическую строку на список неизменяемых строк
                List<string> inputPanelDataStrings = tempBuilder.ToString().Split('\n').ToList<string>();

                // Если первая строка в списке - не заглавие
                if (!InputPanelTextHelper.isDefaultText(inputPanelDataStrings[0]))
                {
                    return false;
                }

                // Удаляем заглавие
                inputPanelDataStrings.Remove(inputPanelDataStrings.First());

                // Если размер сформированного списка не равен заданному
                if (inputPanelDataStrings.Count != requiredDataStringsSize)
                {
                    return false;
                }

                // Проходим по каждой строке компонентов
                foreach (string componentString in inputPanelDataStrings)
                {

                    // Костыль
                    if (componentString == "")
                    {
                        break;
                    }

                    // Если строка данных компонента не содержит пробел или символ '='
                    if (!componentString.Contains(' ') || !componentString.Contains('='))
                    {
                        // Очищаем словарь т.к. в него уже могли добавится данные, без очистки словаря при обрыве его заполнения в нём останется мусор
                        fillableDict.Clear();
                        return false;
                    }


                    // Очищаем строку от пробелов
                    string newComponentString = componentString.Replace(" ", "");

                    // Разбиваем данные компонента по символу '='
                    string[] componentNameAmount = newComponentString.Split('=');

                    // Добавляем в словарь подтип компонента и количество
                    if (InputPanelTextHelper.ComponentNamesRUSubtypesENG.Keys.Contains(componentNameAmount[0]))
                    {
                        DEBUGSTR.Append("ComponentNamesRUSubtypesENG содержит подтип " + componentNameAmount[0]);
                        fillableDict.Add(InputPanelTextHelper.ComponentNamesRUSubtypesENG[componentNameAmount[0]], Int32.Parse(componentNameAmount[1]));
                    }
                    DEBUGSTR.Append("ComponentNamesRUSubtypesENG не содержит подтип " + componentNameAmount[0]);
                }

                return true;
            }
        }


        static class ItemDictionaryFiller
        {
            // Метод заполнения словаря типа "подтип - предмет" предметами, имеющимися в инвентаре
            public static bool fillSubtypeIdItemDictionary(Dictionary<string, MyInventoryItem> fillableDict, string[] subtypeIds, List<MyInventoryItem> items)
            {

                // Если подтипы пусты или инвентарь пуст
                if (subtypeIds.Length < 1 || items.Count < 1)
                {
                    return false;
                }


                // Очищаем словарь для заполнения
                fillableDict.Clear();

                // Проходим по всем предметам
                foreach (MyInventoryItem item in items)
                {

                    // Проходим по всем подтипам предметов
                    foreach (string subtype in InputPanelTextHelper.ComponentNamesRUSubtypesENG.Values)
                    {

                        // Если подтип предмета равен подтипу из имеющихся, то добавляем их в словарь
                        if (item.Type.SubtypeId == subtype) fillableDict.Add(subtype, item);

                    }

                }

                return true;
            }
        }

        class AssemblerManager
        {
            // Сборщик
            public IMyAssembler Assembler { get; set; }

            // Конструктор по умолчанию
            public AssemblerManager(IMyAssembler assembler) => Assembler = assembler;

            public void assembleItem(MyInventoryItem item, int amount, StringBuilder DEBUGSTR)
            {
                DEBUGSTR.Append("MyObjectBuilder_" + item.Type.ToString() + '/' + item.Type.TypeId);

                // Получаем MyDefinitionId из item
                MyDefinitionId defID = MyDefinitionId.Parse("MyObjectBuilder_" + item.Type.ToString() + '/' + item.Type.TypeId);

                // Получаем MyFixedPoint из amount
                MyFixedPoint fpAmount = amount;

                // Добавляем в очередь создание предмета item в количестве amount
                this.Assembler.AddQueueItem(defID, fpAmount);
            }
        }

        // Класс перемещения предметов из одного инвентаря в другой инвентарь
        class SmartItemTransferer
        {

            // Метод перемещения из одного инвентаря в другой с использованием 2-ух словарей: словарь типа "подтип-предмет" и словарь типа "подтип-количество"
            public bool smartTransferTo(IMyInventory startingInventory, IMyInventory destinationInventory, Dictionary<string, MyInventoryItem> subtypesItems, Dictionary<string, int> subtypesAmounts, AssemblerManager asmManager, StringBuilder DEBUGSTR)
            {

                // Если инвентари некорректны или словари пусты
                if (startingInventory == null || destinationInventory == null || subtypesAmounts.Count < 1 || subtypesItems.Count < 1)
                {
                    return false;
                }


                // Проходим по всему словарю
                foreach (string subtype in subtypesItems.Keys)
                {

                    // Если компонент не запрошен 
                    if (subtypesAmounts[subtype] < 0)
                    {
                        // Удаляем данные о нем из словаря subtypesItems
                        subtypesItems.Remove(subtype);

                        // Пропускаем его
                        continue;
                    }


                    // Если в инвентаре недостаточно предметов необходимого типа
                    if (!startingInventory.ContainItems(subtypesAmounts[subtype], subtypesItems[subtype].Type))
                    {

                        // Собираем необходимые предметы
                        asmManager.assembleItem(subtypesItems[subtype], subtypesAmounts[subtype], DEBUGSTR);

                        // Сохраняем глобальный снимок себя
                        SmartItemTransfererSnapshot.saveSnapshot(this, new SmartItemTransfererSnapshot.SmartTransferToSnapshot(startingInventory, destinationInventory, subtypesItems, subtypesAmounts, asmManager, DEBUGSTR));

                        // Устанавливаем приостановленный статус работе
                        Worker.actualWorkState = Worker.WorkStates.Paused;

                        // Возвращаем ответ о том, что мы не перенесли предметы ( просто не закончили )
                        return false;


                    }


                    // Перекидываем предметы в запрошенном количестве в пункт назначения
                    startingInventory.TransferItemTo(destinationInventory, subtypesItems[subtype], subtypesAmounts[subtype]);

                    // Удаляем данные о предмете из словаря subtypesItems
                    subtypesItems.Remove(subtype);
                }


                return true;
            }
        }



        public Program()
        {

            // Берем панель ввода
            IMyTextPanel inputPanel = GridTerminalSystem.GetBlockWithName(InputData.InputPanelName) as IMyTextPanel;
            // Берем панель вывода
            IMyTextPanel outputPanel = GridTerminalSystem.GetBlockWithName(InputData.OutputPanelName) as IMyTextPanel;

            // Устанавливаем панели вывода стандартный вид
            InputPanelTextHelper.setDefaultSurfaceView(outputPanel);

            // Устанавливаем панели ввода стандартный вид и вводим исходные данные
            InputPanelTextHelper.setDefaultSurfaceView(inputPanel);
            InputPanelTextHelper.writeDefaultText(inputPanel);

            // Устанавливаем частоту тиков скрипта на раз в ~1.5 секунды
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {

            StringBuilder DEBUGSTR_1 = new StringBuilder(), DEBUGSTR_2 = new StringBuilder(), DEBUGSTR_3 = new StringBuilder();

            switch (Worker.actualWorkState)
            {
                case Worker.WorkStates.Waiting:

                    // Если передали аргумент "start", то начинаем работу
                    if(argument == "start")
                    {
                       
                        Worker.work(DEBUGSTR_1, DEBUGSTR_2, DEBUGSTR_3);
                    }

                    break;

                case Worker.WorkStates.InProgress:

                    // Ничего не делаем

                    break;

                case Worker.WorkStates.Paused:

                    // Пытаемся восстановить работу
                    Worker.workResumption(DEBUGSTR_1, DEBUGSTR_2, DEBUGSTR_3);

                    break;

                case Worker.WorkStates.Completed:

                    // Меняем частоту тиков скрипта на отсутствие тиков
                    Runtime.UpdateFrequency = UpdateFrequency.None;

                    // Тута выводим лог о завершении работы
                    //

                    // Засыпаем на 3 секунды
                    System.Threading.Thread.Sleep(3000);

                    // Устанавливаем стандартные значения после таймаута
                    // TODO: вынести это и это же из Program() в класс
                    IMyTextPanel inputPanel = GridTerminalSystem.GetBlockWithName(InputData.InputPanelName) as IMyTextPanel;
                    IMyTextPanel outputPanel = GridTerminalSystem.GetBlockWithName(InputData.OutputPanelName) as IMyTextPanel;


                    InputPanelTextHelper.setDefaultSurfaceView(outputPanel);
                    InputPanelTextHelper.setDefaultSurfaceView(inputPanel);
                    InputPanelTextHelper.writeDefaultText(inputPanel);

                    // Возвращаем частоту тиков
                    Runtime.UpdateFrequency = UpdateFrequency.Update100;

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
