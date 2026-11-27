/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: EntitiesArrayBinder.cs
    File description: EntitiesArrayBinder related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated legacy MVC integration metadata and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.0.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using Senparc.Core.Utility;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Threading.Tasks;

namespace Senparc.Mvc
{
    public class EntitiesArrayBinder : IModelBinder
    {
        public string Profix { get; set; }
        /// <summary>
        /// Initializes the legacy form-array model binder.
        /// </summary>
        public EntitiesArrayBinder()
        {
        }

        //public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        /// <summary>
        /// Binds repeated prefixed form fields into a strongly typed
        /// <see cref="List{T}"/> instance.
        /// </summary>
        /// <param name="bindingContext">The MVC model-binding context.</param>
        /// <returns>A task that completes with the constructed model.</returns>
        /// <exception cref="Exception">
        /// Thrown when the target type is not generic or property value counts differ.
        /// </exception>
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            return Task.Factory.StartNew(() => { 
            var bingdingType = bindingContext.ModelType;
            if (!bingdingType.IsGenericType)
            {
                throw new Exception(bingdingType.FullName + "必须为泛型变量！如List<T>");

            }

            var modelType = bingdingType.GetGenericArguments()[0];
            var listType = typeof(List<>).MakeGenericType(modelType);
            Profix = modelType.Name + ".";//Legacy comment translated to English.

            //var obj = bingdingType.Assembly.CreateInstance(bingdingType.FullName);
            var list = Activator.CreateInstance(listType);//Legacy comment translated to English.
            var addMethod = listType.GetMethod("Add");//List<T>.Addmethod

            var formCollection = bindingContext.HttpContext.Request.Form;

            var valueCollection = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            var properties = modelType.GetProperties();
            var recordCount = 0;//Legacy comment translated to English.
            //Legacy comment translated to English.
            foreach (var prop in properties)
            {
                var formKey = Profix + prop.Name;
                var formValues = formCollection[formKey];
                if (formValues.Count > 0)
                {
                    //Legacy comment translated to English.
                    if (recordCount == 0)
                    {
                        //Legacy comment translated to English.
                        recordCount = formValues.Count;
                        //Legacy comment translated to English.
                    }
                    if (formValues.Count != recordCount)
                    {
                        throw new Exception(formKey + "数量与其他属性不一致。第一个属性数量为：" + recordCount + "，当前数量为：" + formValues.Count);
                    }
                    valueCollection.Add(prop.Name, formValues);
                }
            }

            for (int i = 0; i < recordCount; i++)
            {
                //Legacy comment translated to English.
                var entity = Activator.CreateInstance(modelType);
                foreach (var item in valueCollection)
                {
                    ReflectorUtility.SetPropertyValue(entity, modelType, item.Key, item.Value[i]);//Legacy comment translated to English.
                }
                addMethod.Invoke((object)list, new object[] { entity });//Legacy comment translated to English.
            }

            /*return Task*/;
            });
        }
    }
}
