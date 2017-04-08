﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ExcelInterfaces;
using Application = Microsoft.Office.Interop.Excel.Application;


namespace CommonAddin
{
    public class BindingService : IBindingService
    {
        private readonly Dictionary<string,Binding> _bindings = new Dictionary<string, Binding>();

        public void AddBinding<T,TProperty>(string cell, T obj, Expression<Func<T, TProperty>> memberLambda) where T : class
        {
            _bindings[cell] = new Binding<T, TProperty>(cell, obj, memberLambda);
        }

        public void OnValueChanged(string cell, object value)
        {
            if (_bindings.All(x => x.Key != cell))
                return;
            var binding = _bindings.SingleOrDefault(x => x.Key == cell);
            binding.Value?.Set(value);
        }

        public void OnSheetChange(object sheet, object target)
        {
            var range = target as Microsoft.Office.Interop.Excel.Range;
            var address = $"[{range?.Application.ActiveWorkbook.Name}]{range?.Worksheet.Name}{range?.Address}";
            OnValueChanged(address, range?.Value);

            // What to do with cut and paste???
            // the address should update
        }
    }

    public class Binding
    {
        public virtual void Set(object value) { }
    }

    public class Binding<T,TProperty> : Binding where T : class
    {
        // cell reference
        private string _cell;
        // associated object 
        private T _object;
        // function which is associated with the object action cell
        private readonly Action<TProperty> _property;

        public override void Set(object value)
        {
            _property((TProperty) value);
        }

        public Binding(string cell, T obj, Expression<Func<T, TProperty>> memberLamda)
        {
            _cell = cell;
            _object = obj;

            var memberSelectorExpression = memberLamda.Body as MemberExpression;
            if (memberSelectorExpression != null)
            {
                var property = memberSelectorExpression.Member as PropertyInfo;
                if (property != null)
                    _property = value => property.SetValue(obj, value, null);
            }
        }
    }
}