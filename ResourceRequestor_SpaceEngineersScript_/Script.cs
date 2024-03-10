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
//using VRage.Game.ModAPI;
//using  Sandbox.ModAPI;

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
        class ItemTransferer
        {

            // Метод перемещения из одного инвентаря в другой с использованием 2-ух словарей: словарь типа "подтип-предмет" и словарь типа "подтип-количество"
            public bool smartTransferToByDictionaries(IMyInventory startingInventory, IMyInventory destinationInventory, Dictionary<string, MyInventoryItem> subtypesItems, Dictionary<string, int> subtypesAmounts, AssemblerManager asmManager, StringBuilder DEBUGSTR)
            {

                // Если инвентари некорректны или словари пусты
                if (startingInventory == null || destinationInventory == null || subtypesAmounts.Count < 1 || subtypesItems.Count < 1)
                {
                    return false;
                }


                // Проходим по всему словарю
                foreach (string subtype in subtypesItems.Keys)
                {

                    // Если компонент не запрошен, то пропускаем его 
                    if (subtypesAmounts[subtype] < 0) continue;


                    // Если в инвентаре недостаточно предметов необходимого типа
                    if (!startingInventory.ContainItems(subtypesAmounts[subtype], subtypesItems[subtype].Type))
                    {
                        /// Пока пропускаем перенос этого предмета, в будущем здесь будет запрос ресурсов, которых не хватает
                        //continue;
                        // Собираем необходимые предметы
                        asmManager.assembleItem(subtypesItems[subtype], subtypesAmounts[subtype], DEBUGSTR);
                        ///
                    }


                    // Перекидываем предметы в запрошенном количестве в пункт назначения
                    startingInventory.TransferItemTo(destinationInventory, subtypesItems[subtype], subtypesAmounts[subtype]);

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

            // Берем отправной инвентарь
            IMyInventory startingInventory = GridTerminalSystem.GetBlockWithName(InputData.StartingContainerName).GetInventory();
            // Берем инвентарь назначения
            IMyInventory destinationInventory = GridTerminalSystem.GetBlockWithName(InputData.DestinationContainerName).GetInventory();

            // Берем панель ввода
            IMyTextPanel inputPanel = GridTerminalSystem.GetBlockWithName(InputData.InputPanelName) as IMyTextPanel;
            // Берем панель вывода
            IMyTextPanel outputPanel = GridTerminalSystem.GetBlockWithName(InputData.OutputPanelName) as IMyTextPanel;

            // Записываем первую строку в панель ввода
            outputPanel.WriteText("Запрошенные ресурсы из панели ввода: " + '\n', false);

            // Инициализируем парсер
            InputPanelTextParser parser = new InputPanelTextParser();

            // Инициализируем словарь типа "подтип-количество"
            Dictionary<string, int> subtypesAmounts = new Dictionary<string, int>();

            /// DEBUG START
            StringBuilder DEBUGSTR = new StringBuilder();
            /// DEBUG END

            // Если парсер распарсил данные панели ввода в словарь
            if (parser.parseInputPanelText(inputPanel, subtypesAmounts, DEBUGSTR))
            {

                /// DEBUG START
                Echo(DEBUGSTR.ToString());
                Echo("Ключи словаря subtypesAmounts: " + '\n');

                foreach (string subtype in subtypesAmounts.Keys)
                {
                    Echo(subtype + " " + subtypesAmounts[subtype] + '\n');
                }
                /// DEBUG END

                // Инициализируем словарь типа подтип предмета - предмет
                Dictionary<string, MyInventoryItem> subtypesItems = new Dictionary<string, MyInventoryItem>();

                // Инициализируем и заполняем список предметов инвентаря
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                startingInventory.GetItems(items);

                // Заполняем словарь
                ItemDictionaryFiller.fillSubtypeIdItemDictionary(subtypesItems, InputPanelTextHelper.ComponentSubtypes, items);

                /// DEBUG START
                Echo("Ключи словаря subtypesItems: " + '\n');

                foreach (string subtype in subtypesItems.Keys)
                {
                    Echo(subtype + '\n');
                }
                /// DEBUG END

                // Инициализируем переносчик
                ItemTransferer transferer = new ItemTransferer();

                // Переносим запрошенные ресурсы по запрошенному адресу
                if (transferer.transferToByDictionaries(startingInventory, destinationInventory, subtypesItems, subtypesAmounts))
                {
                    Echo("Мы переместили предметы!");
                }
                else
                {
                    Echo("Перемещение предметов не удалось!");
                }

                // TODO: вызывать ItemTransferer

                /// Устаревший код 
                ///foreach (string componentName in parser.ComponentSubtypeIdsComponentAmounts.Keys)
                ///{
                ///    outputPanel.WriteText( "Компонента - " + componentName + " | Запрош. кол-во - " + parser.ComponentSubtypeIdsComponentAmounts[componentName] + '\n', true);
                ///}
                ///
            }

        }
        public void Save()
        {

        }
        #endregion
    }
}
