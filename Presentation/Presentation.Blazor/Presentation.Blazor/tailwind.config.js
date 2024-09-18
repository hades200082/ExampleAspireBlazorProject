/** @type {import('tailwindcss').Config} */
module.exports = {
    prefix: 'tw-',
    content: [
        './**/*.{razor,html,cshtml}',
        '../Presentation.Blazor.Client/**/*.{razor,html,cshtml}',
        './wwwroot/**/*.js',
    ],
    plugins: [
        require('@tailwindcss/forms'),
        require('@tailwindcss/aspect-ratio'),
        require('@tailwindcss/typography'),
    ]
}