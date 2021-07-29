using System;

using Newtonsoft.Json;
using Newtonsoft.Json.UnityConverters;
using Newtonsoft.Json.UnityConverters.Math;
using Newtonsoft.Json.Converters;

namespace ExternalUnityRendering.Serialization
{
    /// <summary>
    /// Used to convert a <see cref="EURScene"/> to JSON. To be used with
    /// <see cref="JsonSerializer"/> and <see cref="JsonSerializerSettings"/>.
    /// </summary>
    public class EURSceneConverter : PartialConverter<EURScene>
    {
        /// <summary>
        /// Converter to serialize <see cref="EURGameObject"/> used in
        /// <see cref="EURScene"/>.
        /// </summary>
        private readonly EURGameObjectConverter _stateConverter = new EURGameObjectConverter();

        /// <summary>
        /// Converter to serialize <see cref="UnityEngine.Vector2Int"/> used in
        /// <see cref="EURScene"/>.
        /// </summary>
        private readonly Vector2IntConverter _vector2IntConverter = new Vector2IntConverter();

        /// <summary>
        /// Converter to serialize <see cref="System.DateTime"/> used in
        /// <see cref="EURScene"/>.
        /// </summary>
        private readonly IsoDateTimeConverter _dateTimeConverter = new IsoDateTimeConverter();

        /// <summary>
        /// Indicate to the serializer whether this converter can read.
        /// </summary>
        public override bool CanRead
        {
            get { return false; }
        }

        /// <summary>
        /// Using <paramref name="name"/>, deserialize and update the corresponding field in
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The deserialized object.</param>
        /// <param name="name">The name of the field/member.</param>
        /// <param name="reader">The <see cref="JsonReader"/> to read the the data of the
        /// field/member.</param>
        /// <param name="serializer"><see cref="JsonSerializer"/> to deserialise nested objects.
        /// </param>
        protected override void ReadValue(ref EURScene value, string name, JsonReader reader, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Convert <paramref name="value"/> to json using <paramref name="writer"/> and
        /// <paramref name="serializer"/>.
        /// </summary>
        /// <param name="writer"><see cref="JsonWriter"/> Where the JSON data is saved. </param>
        /// <param name="value">The data to be serialized.</param>
        /// <param name="serializer"><see cref="JsonSerializer"/> to serialize nested objects.
        /// </param>
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
