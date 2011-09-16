﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game3
{
    /// <summary>
    /// Юнит
    /// </summary>
    [Serializable]
    public class Unit 
    {
        #region Конструкторы
        public Unit()
        {

        }

        public Unit(string typeCode, Map map)
        {
            TypeCode = typeCode;
            Map = map;
            Health = Type.HealthMax;
            State = 1;
            IsPlayer = false;
        }
        #endregion

        #region Свойства юнита
        /// <summary>Личное имя юнита</summary>
        public string Name { get; set; }
        // <summary>Фракция юнита. Юниты из пртивоположных фракций атакуют друг-друга. Если равно 0, то юнит - декорация.</summary>
        public int Fraction { get; set; }
        /// <summary>Статус юнита. Если 0 то юнит мертв</summary>
        public int State { get; set; }
        /// <summary>Теукщее здоровье</summary>
        public float Health { get; set; }
        /// <summary>Позиция юнита </summary>
        public Vector3 Position { get; set; }
        /// <summary>Углы с осями</summary>
        public Vector3 Angles { get; set; }
        /// <summary>Время последней атаки</summary>
        public double LastAttackTime { get; set; }
        /// <summary>Является ли юнит игороком</summary>
        [Obsolete]
        public bool IsPlayer { get; set; }
        /// <summary>Указатель на карту, на которой расположен юнит</summary>
        [XmlIgnore]
        public Map Map { get; set; }
        /// <summary>Код класса юнита </summary>
        public string TypeCode { get; set; }

        private UnitType _type;
        /// <summary>Тип юнита</summary>
        [XmlIgnore]
        public UnitType Type { get { return _type ?? (_type = Map.Workarea.GetUnitType(TypeCode)); } }
        #endregion

        #region Свойства класса
        //public float DamageMin
        //{
        //    get { return Type.DamageMin; }
        //}

        //public float DamageMax
        //{
        //    get { return Type.DamageMax; }
        //}

        //public float Speed
        //{
        //    get { return Type.Speed; }
        //}

        //public float VisibilityRange
        //{
        //    get { return Type.VisibilityRange; }
        //}

        //public float AttackRange
        //{
        //    get { return Type.AttackRange; }
        //}

        //public float AttackDelay
        //{
        //    get { return Type.AttackDelay; }
        //}

        //public float Scale
        //{
        //    get { return Type.Scale; }
        //}

        //[XmlIgnore]
        //public Model Model
        //{
        //    get
        //    {
        //        return Type.Model;
        //    }
        //}
        #endregion

        #region Методы
        /// <summary>
        /// Нарисовать себя
        /// </summary>
        /// <param name="camera"></param>
        public virtual void Draw(ICamera camera)
        {
            if (Type.Model == null)
            {
                Color color = Fraction == 1 ? Color.White : Color.Black;

                VertexPositionColor[] vertexData = new VertexPositionColor[3];
                vertexData[0] = new VertexPositionColor(Position, Color.Red);
                vertexData[1] = new VertexPositionColor(Position + new Vector3(0.0f, 0.5f, 0.0f), color);
                vertexData[2] = new VertexPositionColor(Position + new Vector3(0.5f, 0.0f, 0.5f), color);
                Map.Workarea.Game.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertexData, 0, 1);
            }
            else
            {
                Matrix[] transforms = new Matrix[Type.Model.Bones.Count];
                Type.Model.CopyAbsoluteBoneTransformsTo(transforms);

                foreach (ModelMesh mesh in Type.Model.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.FogEnabled = Map.FogEnabled;
                        effect.FogStart = Workarea.Current.Settings.ForStart;
                        effect.FogEnd = Workarea.Current.Settings.FogEnd;
                        effect.FogColor = Map.FogColor;

                        if(Workarea.Current.Settings.EnableDefaultLighting)
                            effect.EnableDefaultLighting();

                        effect.World = transforms[mesh.ParentBone.Index] * Matrix.CreateScale(Type.Scale) *
                                        Matrix.CreateRotationX(Angles.X) * Matrix.CreateRotationY(Angles.Y) * Matrix.CreateRotationZ(Angles.Z) *
                                        Matrix.CreateTranslation(Position);
                        effect.View = camera.View;
                        effect.Projection = camera.Proj;
                    }
                    mesh.Draw();
                }
            }
        }

        /// <summary>
        /// Основная логика поведения юнита
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void Update(GameTime gameTime)
        {
            if (Health < 0)
            {
                State = 0;
                Health = 0;
                return;
            }

            #region Атака или передвижение к ближайшему врагу
            if (Fraction != 0)
            {
                Unit nearestEnemy = FindNearestVisibleEnemy();

                if (nearestEnemy != null)
                {
                    if (DistanceTo(nearestEnemy) > Type.AttackRange)
                    {
                        //Перемещение
                        Step(nearestEnemy, Type.Speed * (float)gameTime.ElapsedGameTime.TotalSeconds);
                    }
                    else
                    {
                        if ((gameTime.TotalGameTime.TotalSeconds - LastAttackTime) > Type.AttackDelay)
                        {
                            //Атака
                            LastAttackTime = gameTime.TotalGameTime.TotalSeconds;
                            nearestEnemy.Health -= Type.DamageMax;
                        }
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// Вычисление расстояния до заданного юнита
        /// </summary>
        /// <param name="unit">Заданный юнит</param>
        /// <returns>Расстояние</returns>
        public  float DistanceTo(Unit unit)
        {
            return Vector3.Distance(Position, unit.Position);
        }

        /// <summary>
        /// Поиск ближайшего врага
        /// </summary>
        /// <returns></returns>
        public Unit FindNearestVisibleEnemy()
        {
            Unit enemy = null;

            foreach (Unit unit in Map.Units)
            {
                if (unit == this)
                    continue;

                float distance = DistanceTo(unit);
                if (((enemy == null) || (distance < unit.DistanceTo(enemy))) && (distance < Type.VisibilityRange) && (Fraction != unit.Fraction) && (unit.Fraction != 0))
                    enemy = unit;
            }

            return enemy;
        }

        /// <summary>
        /// Перемещает текущий юнит в направлении заданного на заданное расстояние
        /// </summary>
        /// <param name="unit">Заданный юнит</param>
        /// <param name="distance">Расстояние</param>
        public void Step(Unit unit, float distance)
        {
            Vector3 v = Vector3.Normalize(unit.Position - Position);
            Position += v*distance;
        }

        /// <summary>
        /// Проверка столкновения юнита с заданой пирамидой вида
        /// </summary>
        /// <param name="unit">Юнит</param>
        /// <param name="boundingFrustum">Пирамида вида</param>
        /// <returns>Истина если пересекаются</returns>
        public bool Intersects(BoundingFrustum boundingFrustum)
        {
            if (Type.Model == null)
                return true;
            Matrix[] transforms = new Matrix[Type.Model.Bones.Count];
            Type.Model.CopyAbsoluteBoneTransformsTo(transforms);
            return Type.Model.Meshes.Any(mesh => boundingFrustum.Intersects(mesh.BoundingSphere.Transform(Matrix.CreateScale(Type.Scale) * Matrix.CreateTranslation(Position))));
        }
        #endregion

        public override string ToString() { return Name; }
    }
}
