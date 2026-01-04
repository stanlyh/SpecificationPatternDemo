

namespace SpecificationPatternDemo
{
    internal class PostByCategorySpecification
    {
        private string v;

        public PostByCategorySpecification(string v)
        {
            this.v = v;
        }

        internal object Or(PostByCategorySpecification architectureSpec)
        {
            throw new NotImplementedException();
        }
    }
}