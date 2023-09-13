using newstrek.Models;
using System.Reflection;

namespace newstrek.Services
{
    public class MapObjectToListService
    {
        public List<string> MapInterestedTopicToListAsync (List<InterestedTopic> userInterestedTopic)
        {
            List<string> selectedInterestedTopic = new List<string>();

            // Try to iterate the object(must use System.Reflection)
            Type objectType = typeof(InterestedTopic);
            PropertyInfo[] properties = objectType.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                string propertyName = property.Name;
                object propertyValue = property.GetValue(userInterestedTopic[0]);

                // Check if propertyValue is a boolean and true
                if (propertyValue is bool && (bool)propertyValue)
                {
                    // If the value is true, add its key into the List selectedInterestedTopic
                    selectedInterestedTopic.Add(propertyName);
                }
            }

            return selectedInterestedTopic;
        }
    }
}
