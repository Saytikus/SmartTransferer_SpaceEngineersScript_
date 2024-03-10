﻿using System;
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
            // Переменная для определения имени панели ввода
            static public string InputPanelName { get; private set; } = "Input Panel 1";

            // Переменная для определения имени панели вывода
            static public string OutputPanelName { get; private set; } = "Output Panel 1";

            static public string StartingContainerName { get; private set; } = "Starting Container 1";

            static public string DestinationContainerName { get; private set; } = "Destination Container 1";
        }

        /** BlockGetter - класс для получения блоков грида по имени.
         * 
         */
        static class BlockGetter
        {
            /** getPanel - метод получения Text Panel по имени.
             * system - система терминала
             * name - имя блока Text Panel
             * return Объект IMyTextPanel
             */
            public static IMyTextPanel getPanel(IMyGridTerminalSystem system, string name)
            {

                // Возвращаем блок с именем name из грида как IMyTextPanel
                return system.GetBlockWithName(name) as IMyTextPanel;
            }

            public static IMyCargoContainer getContainer(IMyGridTerminalSystem system, string name)
            {

                //
                return system.GetBlockWithName(name) as IMyCargoContainer;
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
            "Бронированное стекло", "Компьютер", "Строительные компоненты", "Компоненты детектора", "Экран", "Взрывчатка",
            "Балка", "Компоненты гравитационного генератора", "Внутренняя пластина", "Большая стальная труба", "Медицинские компоненты", "Компонент решётки", "Мотор",
            "Энергоячейка", "Радиокомпоненты", "Компоненты реактора", "Малая трубка", "Солнечная ячейка", "Стальная пластина", "Сверхпроводник",
            "Детали ионного ускорителя"
            };

            // Словарь имя компонента на русском - подтип компонента
            public static Dictionary<string, string> ComponentNamesRUSubtypesENG { get; private set; } = new Dictionary<string, string>()
        {
            { "Бронированное стекло", "BulletproofGlass" }, { "Компьютер",  "Computer" }, { "Строительные компоненты", "Construction" },
            { "Компоненты детектора", "Detector" }, { "Экран", "Display" }, { "Взрывчатка", "Explosives" },
            { "Балка", "Girder" }, { "Компоненты гравитационного генератора", "GravityGenerator" }, { "Внутренняя пластина", "InteriorPlate" },
            { "Большая стальная труба", "LargeTube" }, { "Медицинские компоненты", "Medical" }, { "Компонент решётки", "MetalGrid" },
            { "Мотор", "Motor" }, { "Энергоячейка", "PowerCell" }, { "Радиокомпоненты", "RadioCommunication" },
            { "Компоненты реактора", "Reactor" }, { "Малая трубка", "SmallTube" }, { "Солнечная ячейка", "SolarCell" },
            { "Стальная пластина", "SteelPlate" }, { "Сверхпроводник", "Superconductor" }, { "Детали ионного ускорителя", "Thrust" }
        };

            public static void setDefaultSurfaceView(IMyTextPanel panel)
            {
                panel.BackgroundColor = Color.Black;
                panel.FontColor = Color.Yellow;
                panel.FontSize = 0.7f;
            }

            public static void writeDefaultText(IMyTextPanel inputPanel)
            {
                inputPanel.WriteText("Список запрашиваемых компонентов: " + '\n',false);

                foreach (string componentNameRU in ComponentNamesRU)
                {
                    inputPanel.WriteText(componentNameRU + " = 0" + '\n', true);
                }
            }
        }

        // Класс-парсер данных компонентов из панели ввода
        class InputPanelTextParser
        {
            // Необходимый размер данных
            public const int requiredDataStringsSize = 22;

            // Словарь подтип-запрошенное кол-во предмета 
            // TODO: Перенести его в параметр метода parseInputPanelText и заполнять по ссылке
            public Dictionary<string, int> ComponentSubtypeIdsComponentAmounts { get; private set; } = new Dictionary<string, int>();

            public bool parseInputPanelText(IMyTextPanel inputPanel)
            {
                // Очищаем словарь имён компонентов и количества предметов
                this.ComponentSubtypeIdsComponentAmounts.Clear();

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
                // Удаляем первую строку(она не нужна, т.к.не содержит данных о каком - либо компоненте)
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
                        this.ComponentSubtypeIdsComponentAmounts.Clear();
                        return false;
                    }


                    // Очищаем строку от пробелов
                    string newComponentString = componentString.Replace(" ", "");

                    // Разбиваем данные компонента по символу '='
                    string[] componentNameAmount = newComponentString.Split('=');

                    // Добавляем в словарь подтип компонента и количество
                    this.ComponentSubtypeIdsComponentAmounts.Add(InputPanelTextHelper.ComponentNamesRUSubtypesENG[componentNameAmount[0]], Int32.Parse(componentNameAmount[1]));
                }

                return true;
            }


            static class ItemDictionaryFiller
            {
                public static bool fillSubtypeIdItemDictionary(Dictionary<string, MyInventoryItem> subtypeIDsItems, List<string> subtypeIds, IMyInventory itemsInventory)
                {
                    // Если подтипы пусты
                    if (subtypeIds.Count < 1)
                    {
                        return false;
                    }

                    // Очищаем словарь для заполнения
                    subtypeIDsItems.Clear();

                    // Берем предметы инвентаря
                    List<MyInventoryItem> items = new List<MyInventoryItem>();
                    itemsInventory.GetItems(items);

                    // Если инвентарь пуст
                    if (items.Count < 1)
                    {
                        return false;
                    }

                    // Проходим по всем предметам
                    foreach (MyInventoryItem item in items)
                    {

                        // Проходим по всем подтипам предметов
                        foreach (string subtypeId in InputPanelTextHelper.ComponentNamesRUSubtypesENG.Values)
                        {

                            // Если подтип предмета равен подтипу из имеющихся, то добавляем их в словарь
                            if (item.Type.SubtypeId == subtypeId) subtypeIDsItems.Add(subtypeId, item);

                        }

                    }

                    return true;
                }
            }

            // Класс переброски предметов из одного инвентаря в другой инвентарь
            class ItemTransferer
            {
                public bool transferToByDictionary(IMyInventory startingInventory, IMyInventory destinationInventory, Dictionary<string, MyInventoryItem> subtypeIDsItems, Dictionary<string, int> componentSubtypeIdsComponentAmounts)
                {

                    // Если инвентари некорректны или словари пусты
                    if (startingInventory == null || destinationInventory == null || componentSubtypeIdsComponentAmounts.Count < 1 || subtypeIDsItems.Count < 1)
                    {
                        return false;
                    }


                    // Проходим по всему словарю
                    foreach (string subtypeId in componentSubtypeIdsComponentAmounts.Keys)
                    {

                        // Если компонент не запрошен, то пропускаем его 
                        if (componentSubtypeIdsComponentAmounts[subtypeId] < 0) continue;


                        // Если в инвентаре недостаточно предметов необходимого типа
                        if(!startingInventory.ContainItems(componentSubtypeIdsComponentAmounts[subtypeId], subtypeIDsItems[subtypeId].Type))
                        {
                            /// Пока останов, в будущем здесь будет запрос ресурсов, которых не хватает
                            return false;
                            ///
                        }


                        // Перекидываем предметы в запрошенном количестве в пункт назначения
                        startingInventory.TransferItemTo(destinationInventory, subtypeIDsItems[subtypeId], componentSubtypeIdsComponentAmounts[subtypeId]);

                    }


                    return true;
                }
            }
        }



        public Program()
        {
            // Устанавливаем частоту тиков скрипта на 60 раз в секунду
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
       
            // Берем отправной инвентарь
            IMyInventory startingInventory = BlockGetter.getContainer(GridTerminalSystem, InputData.StartingContainerName).GetInventory();
            // Берем инвентарь назначения
            IMyInventory destinationInventory = BlockGetter.getContainer(GridTerminalSystem, InputData.DestinationContainerName).GetInventory();

            // Берем панель ввода
            IMyTextPanel inputPanel = BlockGetter.getPanel(GridTerminalSystem, InputData.InputPanelName);
            // Берем панель вывода
            IMyTextPanel outputPanel = BlockGetter.getPanel(GridTerminalSystem, InputData.OutputPanelName);

            // Устанавливаем панели вывода стандартный вид
            InputPanelTextHelper.setDefaultSurfaceView(outputPanel);

            // Устанавливаем панели ввода стандартный вид и вводим исходные данные
            InputPanelTextHelper.setDefaultSurfaceView(inputPanel);
            InputPanelTextHelper.writeDefaultText(inputPanel);

            // Записываем первую строку в панель ввода
            outputPanel.WriteText("Полученные данные из панели ввода: " + '\n', false);

            // Инициализируем парсер
            InputPanelTextParser parser = new InputPanelTextParser();

            // Если парсер распарсил данные панели ввода
            if (parser.parseInputPanelText(inputPanel))
            {

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