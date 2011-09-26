﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace Game3
{
    /// <summary>
    /// Игрок
    /// </summary>
    public class Player:Unit
    {
        private BoundingSphere _boundingSphere;
        public const float Growth = 1.8f;

        public Player(string typeCode, Map map):base(typeCode,map)
        {
            //_boundingSphere = new BoundingSphere(Position, 0.1f);
        }

        /// <summary>
        /// Ничего не рисовать
        /// </summary>
        /// <param name="camera">Камера</param>
        public override void Draw(ICamera camera)
        {
           
        }

        /// <summary>
        /// Управление игроком с помощью клавиатуры и мыши
        /// </summary>
        /// <param name="gameTime">Игровое время</param>
        public override void Update(GameTime gameTime)
        {
            Form frm = Control.FromHandle(Map.Workarea.Game.Window.Handle) as Form;
            if (!frm.Focused)
            {
                return;
            }

            #region Angles
            const int turnSpeed = 5;

            int centerX = Map.Workarea.Game.GraphicsDevice.Viewport.Width / 2;
            int centerY = Map.Workarea.Game.GraphicsDevice.Viewport.Height / 2;

            MouseState mouseState = Mouse.GetState();
            Mouse.SetPosition(centerX, centerY);

            float seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            float yaw = MathHelper.ToRadians((mouseState.X - centerX) * seconds * Workarea.Current.Settings.MouseSpeedX);
            float pitch = MathHelper.ToRadians((mouseState.Y - centerY) * seconds * Workarea.Current.Settings.MouseSpeedY);

            Angles = new Vector3(MathHelper.Clamp(Angles.X + pitch, MathHelper.ToRadians(-90f), MathHelper.ToRadians(90f)), Angles.Y + yaw, Angles.Z);
            #endregion

            #region Position
            Vector3 forward = -Vector3.Normalize(new Vector3(
                                             (float)Math.Sin(-Angles.Y) * (float)Math.Cos(Angles.X),
                                             (float)Math.Sin(Angles.X),
                                             (float)Math.Cos(-Angles.Y) * (float)Math.Cos(Angles.X)));

            Vector3 left = -Vector3.Normalize(new Vector3(
                                             (float)Math.Cos(Angles.Y),
                                             0f,
                                             (float)Math.Sin(Angles.Y)));

            Vector3 oldPosition=Position;


            KeyboardState state = Keyboard.GetState();

            if (state.IsKeyDown(Keys.W))
                Position += forward * seconds * Type.Speed;
            if (state.IsKeyDown(Keys.S))
                Position -= forward * seconds * Type.Speed;
            if (state.IsKeyDown(Keys.A))
                Position += left * seconds * Type.Speed;
            if (state.IsKeyDown(Keys.D))
                Position -= left * seconds * Type.Speed;
            if (state.IsKeyDown(Keys.Space) && IsOnGround(Growth))
                Impulse += JumpAcceleration;
            if (state.IsKeyDown(Keys.C))
                Position += Vector3.Down * seconds * Type.Speed;

            //Проверка столкновений игрока
            if (Map.Intersects(this))
                Position = oldPosition;

            #endregion

            UpdatePhysics(gameTime, Growth);
        }

        /// <summary>
        /// Проверка столкновения игрока с заданным юнитом. Игрок представляется сферой с радиусом 0.2
        /// </summary>
        /// <param name="unit">Заданный юнит</param>
        /// <returns></returns>
        public override bool Intersects(Unit unit)
        {
            if (unit == this)
                return false;

            _boundingSphere = new BoundingSphere(Position, 0.2f);
            return unit.Intersects(_boundingSphere);
        }

        public override string ToString() { return Name; }
    }
}
