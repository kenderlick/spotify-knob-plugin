using System;
using System.Text;

namespace SpotifyKnob
{
    public static class SvgRenderer
    {
        public static string EscapeXml(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("&", "&amp;")
                      .Replace("<", "&lt;")
                      .Replace(">", "&gt;")
                      .Replace("\"", "&quot;")
                      .Replace("'", "&apos;");
        }

        public static string Truncate(string str, int maxLen)
        {
            if (string.IsNullOrEmpty(str)) return "";
            if (str.Length <= maxLen) return str;
            return str.Substring(0, maxLen - 1) + "…";
        }

        private static string ToBase64Svg(string svgXml)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(svgXml);
            return "data:image/svg+xml;base64," + Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Renders large clean Spotify logo filling the entire canvas with transparency.
        /// </summary>
        public static string RenderLogoOnly()
        {
            string svg =
                "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"144\" height=\"144\" viewBox=\"0 0 144 144\">" +
                "<circle cx=\"72\" cy=\"72\" r=\"68\" fill=\"#1DB954\"/>" +
                "<path d=\"M34 50 C56 36, 88 36, 110 51\" stroke=\"#121212\" stroke-width=\"10\" stroke-linecap=\"round\" fill=\"none\"/>" +
                "<path d=\"M40 70 C60 60, 84 60, 104 71\" stroke=\"#121212\" stroke-width=\"8.5\" stroke-linecap=\"round\" fill=\"none\"/>" +
                "<path d=\"M46 90 C62 82, 82 82, 98 91\" stroke=\"#121212\" stroke-width=\"7.5\" stroke-linecap=\"round\" fill=\"none\"/>" +
                "</svg>";

            return ToBase64Svg(svg);
        }

        /// <summary>
        /// Renders composite view: transparent background + extra large logo + scrolling title & artist + top bar status indicator.
        /// </summary>
        public static string RenderComposite(
            bool isRunning,
            bool isPlaying,
            string title,
            string artist,
            int marqueePixelOffset,
            string overlayType,
            double overlayOpacity,
            int volumePercent,
            bool isTrackMode = false)
        {
            if (!isRunning)
            {
                return RenderLogoOnly();
            }

            var sb = new StringBuilder(2400);
            sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"144\" height=\"144\" viewBox=\"0 0 144 144\">");
            sb.Append("<defs>");
            sb.Append("<clipPath id=\"tc\"><rect x=\"4\" y=\"28\" width=\"136\" height=\"46\"/></clipPath>");
            sb.Append("<filter id=\"ds\" x=\"-20%\" y=\"-20%\" width=\"140%\" height=\"140%\">");
            sb.Append("<feDropShadow dx=\"0\" dy=\"2\" stdDeviation=\"3\" flood-color=\"#000000\" flood-opacity=\"0.95\"/>");
            sb.Append("</filter>");
            sb.Append("</defs>");

            // Extra Large Spotify Logo watermark in background (100% transparent background)
            sb.Append("<g opacity=\"0.38\">");
            sb.Append("<circle cx=\"72\" cy=\"72\" r=\"68\" fill=\"#1DB954\"/>");
            sb.Append("<path d=\"M34 50 C56 36, 88 36, 110 51\" stroke=\"#121212\" stroke-width=\"10\" stroke-linecap=\"round\" fill=\"none\"/>");
            sb.Append("<path d=\"M40 70 C60 60, 84 60, 104 71\" stroke=\"#121212\" stroke-width=\"8.5\" stroke-linecap=\"round\" fill=\"none\"/>");
            sb.Append("<path d=\"M46 90 C62 82, 82 82, 98 91\" stroke=\"#121212\" stroke-width=\"7.5\" stroke-linecap=\"round\" fill=\"none\"/>");
            sb.Append("</g>");

            // Top Status Bar Indicator (Playing: ▶, Paused: ⏸, Track Mode: ⏮ ⏭)
            if (isTrackMode)
            {
                // Track Mode: compact green badge with ⏮ ⏭
                sb.Append("<g filter=\"url(#ds)\">");
                sb.Append("<rect x=\"38\" y=\"5\" width=\"68\" height=\"18\" rx=\"9\" fill=\"#1DB954\"/>");
                sb.Append("<text x=\"72\" y=\"18\" fill=\"#121212\" font-family=\"-apple-system, Segoe UI, Roboto, Helvetica, Arial, sans-serif\" font-size=\"11\" font-weight=\"900\" text-anchor=\"middle\" letter-spacing=\"2\">⏮ ⏭</text>");
                sb.Append("</g>");
            }
            else if (isPlaying)
            {
                // Playing: small green play badge
                sb.Append("<g filter=\"url(#ds)\" transform=\"translate(63, 5)\">");
                sb.Append("<circle cx=\"9\" cy=\"9\" r=\"9\" fill=\"#1DB954\" fill-opacity=\"0.95\"/>");
                sb.Append("<polygon points=\"7,5 13,9 7,13\" fill=\"#121212\"/>");
                sb.Append("</g>");
            }
            else
            {
                // Paused: small pause badge
                sb.Append("<g filter=\"url(#ds)\" transform=\"translate(63, 5)\">");
                sb.Append("<circle cx=\"9\" cy=\"9\" r=\"9\" fill=\"#222222\" fill-opacity=\"0.9\" stroke=\"#1DB954\" stroke-width=\"1.8\"/>");
                sb.Append("<rect x=\"6\" y=\"5\" width=\"2\" height=\"8\" rx=\"0.8\" fill=\"#1DB954\"/>");
                sb.Append("<rect x=\"10\" y=\"5\" width=\"2\" height=\"8\" rx=\"0.8\" fill=\"#1DB954\"/>");
                sb.Append("</g>");
            }

            string rawTitle = string.IsNullOrEmpty(title) ? "" : title;
            string escTitle = EscapeXml(rawTitle);
            string escArtist = EscapeXml(string.IsNullOrEmpty(artist) ? "Spotify" : artist);

            // Song Title (White, bold 19px, drop-shadow, marquee if > 10 chars)
            if (rawTitle.Length <= 10)
            {
                sb.AppendFormat("<text x=\"72\" y=\"62\" fill=\"#FFFFFF\" filter=\"url(#ds)\" font-family=\"-apple-system, Segoe UI, Roboto, Helvetica, Arial, sans-serif\" font-size=\"19\" font-weight=\"800\" text-anchor=\"middle\">{0}</text>", escTitle);
            }
            else
            {
                int xPos = 10 - marqueePixelOffset;
                sb.AppendFormat("<g clip-path=\"url(#tc)\"><text x=\"{0}\" y=\"62\" fill=\"#FFFFFF\" filter=\"url(#ds)\" font-family=\"-apple-system, Segoe UI, Roboto, Helvetica, Arial, sans-serif\" font-size=\"19\" font-weight=\"800\">{1}</text></g>", xPos, escTitle);
            }

            // Artist Name (Green, bold 18px, drop-shadow)
            string trArtist = Truncate(escArtist, 14);
            sb.AppendFormat("<text x=\"72\" y=\"104\" fill=\"#1DB954\" filter=\"url(#ds)\" font-family=\"-apple-system, Segoe UI, Roboto, Helvetica, Arial, sans-serif\" font-size=\"18\" font-weight=\"800\" text-anchor=\"middle\">{0}</text>", trArtist);

            // Transient Overlays (Volume feedback and App Opening)
            if (!string.IsNullOrEmpty(overlayType) && overlayOpacity > 0.01)
            {
                double op = Math.Min(1.0, Math.Max(0.0, overlayOpacity));
                sb.AppendFormat("<g opacity=\"{0:0.##}\">", op);

                if (overlayType == "volume")
                {
                    int vol = Math.Max(0, Math.Min(100, volumePercent));
                    int barW = (int)Math.Round(104.0 * vol / 100.0);
                    if (barW < 4 && vol > 0) barW = 4;

                    sb.Append("<rect width=\"144\" height=\"144\" rx=\"22\" fill=\"#121212\" fill-opacity=\"0.95\"/>");
                    sb.Append("<text x=\"72\" y=\"28\" fill=\"#1DB954\" font-family=\"-apple-system, Segoe UI, Roboto, Helvetica, Arial, sans-serif\" font-size=\"12\" font-weight=\"700\" text-anchor=\"middle\" letter-spacing=\"2\">VOLUME</text>");
                    sb.AppendFormat("<text x=\"72\" y=\"82\" fill=\"#FFFFFF\" font-family=\"-apple-system, Segoe UI, Roboto, Helvetica, Arial, sans-serif\" font-size=\"44\" font-weight=\"800\" text-anchor=\"middle\">{0}%</text>", vol);
                    sb.Append("<rect x=\"20\" y=\"106\" width=\"104\" height=\"8\" rx=\"4\" fill=\"#282828\"/>");
                    sb.AppendFormat("<rect x=\"20\" y=\"106\" width=\"{0}\" height=\"8\" rx=\"4\" fill=\"#1DB954\"/>", barW);
                }
                else if (overlayType == "opening")
                {
                    sb.Append("<rect width=\"144\" height=\"144\" rx=\"22\" fill=\"#121212\" fill-opacity=\"0.92\"/>");
                    sb.Append("<circle cx=\"72\" cy=\"50\" r=\"22\" stroke=\"#1DB954\" stroke-width=\"4\" fill=\"none\" stroke-dasharray=\"30 15\"/>");
                    sb.Append("<text x=\"72\" y=\"98\" fill=\"#1DB954\" font-family=\"-apple-system, Segoe UI, Roboto, Helvetica, Arial, sans-serif\" font-size=\"13\" font-weight=\"800\" text-anchor=\"middle\">AVVIO...</text>");
                    sb.Append("<text x=\"72\" y=\"118\" fill=\"#888888\" font-family=\"-apple-system, Segoe UI, Roboto, Helvetica, Arial, sans-serif\" font-size=\"11\" font-weight=\"500\" text-anchor=\"middle\">Apertura Spotify</text>");
                }

                sb.Append("</g>");
            }

            sb.Append("</svg>");
            return ToBase64Svg(sb.ToString());
        }
    }
}
