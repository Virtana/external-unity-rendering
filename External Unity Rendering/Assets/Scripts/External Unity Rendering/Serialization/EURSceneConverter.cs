using System;
using Newtonsoft.Json;
using Newtonsoft.Json.UnityConverters;
using Newtonsoft.Json.UnityConverters.Math;
using Newtonsoft.Json.Converters;

namespace ExternalUnityRendering.Serialization
{
    public class EURSceneConverter : PartialConverter<EURScene>
    {
        public override bool CanRead
        {
            get { return false; }
        }

        private readonly EURGameObjectConverter _stateConverter = new EURGameObjectConverter();
        private readonly Vector2IntConverter _vector2IntConverter = new Vector2IntConverter();
        private readonly IsoDateTimeConverter _dateTimeConverter = new IsoDateTimeConverter();

        protected override void ReadValue(ref EURScene value, string name, JsonReader reader, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        protected override void WriteJsonProperties(JsonWriter writer, EURScene value, JsonSerializer serializer)
        {
            writer.WritePropertyName(nameof(value.ExportDate));
            _dateTimeConverter.WriteJson(writer, value.ExportDate, serializer);

            writer.WritePropertyName(nameof(value.SceneRoot));
            _stateConverter.WriteJson(writer, value.SceneRoot, serializer);

            writer.WritePropertyName(nameof(value.RendererSettings));
            writer.WriteStartObject();
            {
                writer.WritePropertyName(nameof(value.RendererSettings.RenderSize));
                _vector2IntConverter.WriteJson(writer, value.RendererSettings.RenderSize, serializer);

                writer.WritePropertyName(nameof(value.RendererSettings.RenderDirectory));
                writer.WriteValue(value.RendererSettings.RenderDirectory);
            }
            writer.WriteEndObject();

            writer.WritePropertyName(nameof(value.ContinueImporting));
            writer.WriteValue(value.ContinueImporting);
        }
    }
}
